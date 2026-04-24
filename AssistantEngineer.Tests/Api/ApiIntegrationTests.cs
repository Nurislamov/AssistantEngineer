using System.Net.Http.Json;
using System.Net;
using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests;

public class ApiIntegrationTests
{
    [Fact]
    public async Task UnversionedApiRouteIsNotMapped()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reports/buildings/0/heating?method=En12831");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetV1HeatingReportReturnsReport()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/reports/buildings/0/heating?method=En12831");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<HeatingReport>();
        Assert.NotNull(report);
        Assert.Equal("Integration project", report.ProjectName);
        Assert.Equal("Integration building", report.BuildingName);
        Assert.Equal(1, report.RoomsCount);
        Assert.True(report.TotalDesignHeatingLoadW > 0);
        Assert.True(response.Headers.TryGetValues("api-supported-versions", out var supportedVersions));
        Assert.Contains("1.0", supportedVersions);
    }

    [Fact]
    public async Task GetEnergyBalanceReturnsAnnualBalance()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/buildings/0/energy-balance?coolingMethod=Simplified&heatingMethod=En12831");

        response.EnsureSuccessStatusCode();
        var balance = await response.Content.ReadFromJsonAsync<BuildingEnergyBalanceResult>();
        Assert.NotNull(balance);
        Assert.Equal("Integration building", balance.BuildingName);
        Assert.NotEmpty(balance.MonthlyBalances);
        Assert.True(balance.AnnualTotalDemandKWh > 0);
    }

    [Fact]
    public async Task GetIso52016BuildingCoolingLoadReturnsThermalZoneBreakdown()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/buildings/0/cooling-load?method=Iso52016");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BuildingCalculationResult>();
        Assert.NotNull(result);
        Assert.Single(result.ThermalZones);
        Assert.Equal("Office zone", result.ThermalZones[0].ThermalZoneName);
        Assert.Equal(1, result.ThermalZones[0].RoomsCount);
    }

    [Fact]
    public async Task GetBuildingCalculationWithUndefinedMethodReturnsBadRequest()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/buildings/0/cooling-load?method=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("validation_failed", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
        Assert.Contains("method", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetEnergyBalanceExcelReturnsWorkbook()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/reports/buildings/0/energy-balance/excel?coolingMethod=Simplified&heatingMethod=En12831");

        response.EnsureSuccessStatusCode();
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.True(content.Length > 0);
    }

    [Fact]
    public async Task PostEnergyPlusBenchmarkUsesRunnerStubWithoutFileSystemValidation()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var request = new EnergyPlusBenchmarkRequest
        {
            ModelArtifactId = "fake-model.idf",
            WeatherArtifactId = "fake-weather.epw",
            RunName = "fake-output"
        };

        var response = await client.PostAsJsonAsync("/api/v1/benchmarks/energyplus", request);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EnergyPlusBenchmarkResult>();
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(request.RunName, result.RunArtifactId);
    }

    [Fact]
    public async Task PostEnergyPlusModelExportCreatesIdfFile()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            await using var factory = new AssistantEngineerApiFactory();
            var client = factory.CreateClient();
            var response = await client.PostAsJsonAsync(
                "/api/v1/benchmarks/energyplus/buildings/0/model",
                new EnergyPlusModelExportRequest { RunName = "integration-building" });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EnergyPlusModelExportResult>();
            Assert.NotNull(result);
            Assert.Equal("Integration building", result.BuildingName);
            Assert.False(string.IsNullOrWhiteSpace(result.ModelArtifactId));

            var artifacts = factory.Services.GetRequiredService<IEnergyPlusArtifactStore>();
            var artifact = artifacts.GetModelArtifact(result.ModelArtifactId);
            Assert.True(artifact.IsSuccess, artifact.Error);
            Assert.True(File.Exists(artifact.Value.FileSystemPath));

            var idf = await File.ReadAllTextAsync(artifact.Value.FileSystemPath);
            Assert.Contains("Office_101", idf);
            Assert.Contains("ZoneHVAC:IdealLoadsAirSystem", idf);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetIso52016ReferenceCasesReturnsLockedBenchmarkResults()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/benchmarks/iso52016/reference-cases");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<Iso52016ReferenceBenchmarkResult>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.All(result, benchmark => Assert.True(benchmark.Passed));
        Assert.Contains(result, benchmark => benchmark.CaseId == "reference-box-heating");
        Assert.Contains(result, benchmark => benchmark.CaseId == "reference-box-cooling");
        Assert.Contains(result, benchmark => benchmark.CaseId == "reference-solar-shading");
    }

    [Fact]
    public async Task GetBuildingArchetypesUsesDedicatedResourceRoute()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/building-archetypes");

        response.EnsureSuccessStatusCode();
        var archetypes = await response.Content.ReadFromJsonAsync<PagedResponse<BuildingArchetypeSummary>>();
        Assert.NotNull(archetypes);
        Assert.NotEmpty(archetypes.Items);
        Assert.True(archetypes.TotalCount >= archetypes.Items.Count);
    }

    [Fact]
    public async Task GetBuildingsByProjectSupportsPaginationSearchAndSorting()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/projects/0/buildings?sortBy=name&page=2&pageSize=1");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<BuildingResponse>>();
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Single(page.Items);
        Assert.Equal("Integration building", page.Items[0].Name);

        var searchResponse = await client.GetAsync("/api/v1/projects/0/buildings?search=annex");

        searchResponse.EnsureSuccessStatusCode();
        var searchPage = await searchResponse.Content.ReadFromJsonAsync<PagedResponse<BuildingResponse>>();
        Assert.NotNull(searchPage);
        Assert.Single(searchPage.Items);
        Assert.Equal("Annex building", searchPage.Items[0].Name);
    }

    [Fact]
    public async Task GetThermalZonesByBuildingSupportsSearchAndSorting()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/buildings/1/thermal-zones?search=meeting&sortBy=name&sortDescending=true");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ThermalZoneResponse>>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("Meeting zone", page.Items[0].Name);
    }

    [Fact]
    public async Task GetEquipmentCatalogSupportsFilteringSortingAndMaxPageSize()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/equipment-catalog?systemType=Split&isActive=true&sortBy=nominalCoolingCapacityKw&sortDescending=true&pageSize=500");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<EquipmentCatalogItemResponse>>();
        Assert.NotNull(page);
        Assert.Equal(100, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("AeroMax 500", page.Items[0].ModelName);
        Assert.All(page.Items, item =>
        {
            Assert.Equal("Split", item.SystemType);
            Assert.True(item.IsActive);
        });
    }

    [Fact]
    public async Task PostProjectWithInvalidBodyReturnsValidationProblem()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/projects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("validation_failed", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
        Assert.Contains("Name", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetReportForUnknownBuildingReturnsNotFound()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/reports/buildings/999?method=Simplified");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Not found", problem.Title);
        Assert.Equal("resource_not_found", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
    }

    [Fact]
    public async Task GetCoolingReportWithPartialEquipmentSelectionReturnsBadRequest()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/reports/buildings/0?method=Simplified&systemType=Split");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Both systemType and unitType", problem.Detail);
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
    }

    [Fact]
    public async Task PostEpwImportWithoutFileReturnsValidationProblemContract()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new StringContent("2020"), "year" }
        };

        var response = await client.PostAsync("/api/v1/climate-zones/0/annual-climate-data/epw", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("validation_failed", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
        Assert.Contains(problem.Errors.Keys, key => string.Equals(key, "sourceFile", StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetExtensionValue(ProblemDetails problem, string key) =>
        problem.Extensions.TryGetValue(key, out var value)
            ? value switch
            {
                string text => text,
                JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
                JsonElement json => json.ToString(),
                _ => value?.ToString()
            }
            : null;

    private sealed class AssistantEngineerApiFactory : WebApplicationFactory<Program>
    {
        private readonly Building _building;
        private readonly IReadOnlyList<Building> _buildings;
        private readonly IReadOnlyList<CoolingEquipmentCatalogItem> _equipmentCatalogItems;
        private readonly IReadOnlyList<ClimateData> _climateData;

        public AssistantEngineerApiFactory()
        {
            var project = DomainInvariantTests.CreateProject("Integration project");
            var climateZone = ClimateZone.Create(
                "Integration climate",
                Temperature.FromCelsius(35).Value,
                Temperature.FromCelsius(-12).Value).Value;
            _building = Building.Create("Integration building", project, climateZone).Value;
            SetEntityId(_building, 0);
            Assert.True(project.AddBuilding(_building).IsSuccess);

            var floor = _building.AddFloor("Level 1").Value;
            var room = floor.AddRoom(
                "Office 101",
                Area.FromSquareMeters(20).Value,
                3,
                Temperature.FromCelsius(22).Value,
                Temperature.FromCelsius(34).Value,
                peopleCount: 2,
                equipmentLoad: Power.FromWatts(400).Value,
                lightingLoad: Power.FromWatts(200).Value).Value;
            SetEntityId(room, 101);
            Assert.True(room.AddWall(
                Area.FromSquareMeters(12).Value,
                isExternal: true,
                ThermalTransmittance.FromValue(1.2).Value,
                CardinalDirection.South).IsSuccess);
            Assert.True(_building.AddThermalZone("Office zone", [room]).IsSuccess);
            Assert.True(room.AddWindow(
                Area.FromSquareMeters(3).Value,
                ThermalTransmittance.FromValue(2).Value,
                SolarHeatGainCoefficient.FromValue(0.5).Value,
                CardinalDirection.South).IsSuccess);
            var officeZone = _building.ThermalZones.Single();
            SetEntityId(officeZone, 100);

            var annexClimateZone = ClimateZone.Create(
                "Annex climate",
                Temperature.FromCelsius(33).Value,
                Temperature.FromCelsius(-8).Value).Value;
            var annexBuilding = Building.Create("Annex building", project, annexClimateZone).Value;
            SetEntityId(annexBuilding, 1);
            Assert.True(project.AddBuilding(annexBuilding).IsSuccess);
            var annexFloor = annexBuilding.AddFloor("Annex level").Value;
            var meetingRoom = annexFloor.AddRoom(
                "Meeting 201",
                Area.FromSquareMeters(16).Value,
                3,
                Temperature.FromCelsius(22).Value,
                Temperature.FromCelsius(32).Value,
                type: RoomType.MeetingRoom).Value;
            var supportRoom = annexFloor.AddRoom(
                "Support 202",
                Area.FromSquareMeters(12).Value,
                3,
                Temperature.FromCelsius(21).Value,
                Temperature.FromCelsius(32).Value,
                type: RoomType.Corridor).Value;
            SetEntityId(meetingRoom, 201);
            SetEntityId(supportRoom, 202);
            Assert.True(annexBuilding.AddThermalZone("Meeting zone", [meetingRoom]).IsSuccess);
            Assert.True(annexBuilding.AddThermalZone("Support zone", [supportRoom]).IsSuccess);
            SetEntityId(annexBuilding.ThermalZones.First(zone => zone.Name == "Meeting zone"), 301);
            SetEntityId(annexBuilding.ThermalZones.First(zone => zone.Name == "Support zone"), 302);

            _buildings = [_building, annexBuilding];
            _equipmentCatalogItems =
            [
                CreateCatalogItem(1, "Aero", "Split", "Wall", "AeroMax 500", 5.0, isActive: true),
                CreateCatalogItem(2, "Aero", "Split", "Cassette", "AeroLite 350", 3.5, isActive: true),
                CreateCatalogItem(3, "Ventis", "VRF", "Ducted", "Ventis Pro", 7.2, isActive: false)
            ];

            _climateData =
            [
                CreateClimateData(climateZone, month: 1),
                CreateClimateData(climateZone, month: 7)
            ];
        }

        private static void SetEntityId(object entity, int id)
        {
            var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(entity, id);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBuildingRepository>();
                services.RemoveAll<IBuildingHeatingReadModelRepository>();
                services.RemoveAll<ICalculationPreferencesRepository>();
                services.RemoveAll<IEquipmentCatalogRepository>();
                services.RemoveAll<IClimateDataRepository>();
                services.RemoveAll<IEnergyPlusBenchmarkRunner>();

                services.AddScoped<IBuildingRepository>(_ => new BuildingRepositoryStub(_buildings));
                services.AddScoped<IBuildingHeatingReadModelRepository>(_ => new BuildingHeatingReadModelRepositoryStub(_building));
                services.AddScoped<ICalculationPreferencesRepository, EmptyPreferencesRepository>();
                services.AddScoped<IEquipmentCatalogRepository>(_ => new EquipmentCatalogRepositoryStub(_equipmentCatalogItems));
                services.AddScoped<IClimateDataRepository>(_ => new ClimateDataRepositoryStub(_climateData));
                services.AddScoped<IEnergyPlusBenchmarkRunner, EnergyPlusBenchmarkRunnerStub>();
            });
        }

        private static ClimateData CreateClimateData(ClimateZone climateZone, int month)
        {
            var climateData = ClimateData.Create(climateZone, month, dayOfMonth: 15, dailyTemperatureRange: 10).Value;
            for (var hour = 0; hour < 24; hour++)
            {
                Assert.True(climateData.AddHourlyData(
                    hour,
                    dryBulbTemp: 30,
                    directSolar: 100,
                    diffuseSolar: 20).IsSuccess);
            }

            return climateData;
        }

        private static CoolingEquipmentCatalogItem CreateCatalogItem(
            int id,
            string manufacturer,
            string systemType,
            string unitType,
            string modelName,
            double capacityKw,
            bool isActive)
        {
            var item = CoolingEquipmentCatalogItem.Create(
                manufacturer,
                systemType,
                unitType,
                modelName,
                Power.FromWatts(capacityKw * 1000).Value,
                isActive).Value;
            SetEntityId(item, id);
            return item;
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"assistant-engineer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly IReadOnlyList<Building> _buildings;

        public BuildingRepositoryStub(IReadOnlyList<Building> buildings) => _buildings = buildings;

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(
                building => building.ThermalZones.Any(zone => zone.Id == thermalZoneId)));

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>(
                _buildings.Where(building => building.ProjectId == projectId).ToArray());

        public void Add(Building building) => throw new NotSupportedException();
    }

    private sealed class EmptyPreferencesRepository : ICalculationPreferencesRepository
    {
        public Task<CalculationPreferences?> GetByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);
    }

    private sealed class EquipmentCatalogRepositoryStub : IEquipmentCatalogRepository
    {
        private readonly IReadOnlyList<CoolingEquipmentCatalogItem> _items;

        public EquipmentCatalogRepositoryStub(IReadOnlyList<CoolingEquipmentCatalogItem> items)
        {
            _items = items;
        }

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListActiveByTypeAsync(
            string systemType,
            string unitType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CoolingEquipmentCatalogItem>>(
                _items.Where(item => item.IsActive && item.SystemType == systemType && item.UnitType == unitType).ToArray());

        public Task<CoolingEquipmentCatalogItem?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items);

        public void Add(CoolingEquipmentCatalogItem item) => throw new NotSupportedException();
    }

    private sealed class BuildingHeatingReadModelRepositoryStub : IBuildingHeatingReadModelRepository
    {
        private readonly BuildingHeatingReadModel _building;

        public BuildingHeatingReadModelRepositoryStub(Building building)
        {
            _building = new BuildingHeatingReadModel(
                building.Id,
                building.Name,
                building.ProjectId,
                building.Project.Name,
                building.ClimateZone?.WinterDesignTemperature.Celsius,
                building.Floors
                    .SelectMany(floor => floor.Rooms)
                    .Select(room => new RoomHeatingReadModel(
                        room.Id,
                        room.Name,
                        room.Area.SquareMeters,
                        room.HeightM,
                        room.IndoorTemperature.Celsius,
                        room.OutdoorTemperatureOverride?.Celsius,
                        room.VentilationParameters is null
                            ? null
                            : new HeatingVentilationReadModel(
                                room.VentilationParameters.AirChangesPerHour,
                                room.VentilationParameters.HeatRecoveryEfficiency,
                                room.VentilationParameters.InfiltrationAirChangesPerHour,
                                room.VentilationParameters.StackCoefficient),
                        room.Windows
                            .Select(window => new WindowHeatingReadModel(
                                window.Area.SquareMeters,
                                window.UValue.Value))
                            .ToList(),
                        room.Walls
                            .Select(wall => new WallHeatingReadModel(
                                wall.Area.SquareMeters,
                                wall.IsExternal,
                                wall.UValue.Value,
                                wall.ConstructionAssembly?.Layers
                                    .Select(layer => new ConstructionLayerHeatingReadModel(
                                        layer.ThicknessM,
                                        layer.Material.ThermalConductivityWPerMK))
                                    .ToList() ??
                                []))
                            .ToList()))
                    .ToList());
        }

        public Task<BuildingHeatingReadModel?> GetByIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BuildingHeatingReadModel?>(buildingId == _building.BuildingId ? _building : null);
    }

    private sealed class ClimateDataRepositoryStub : IClimateDataRepository
    {
        private readonly IReadOnlyList<ClimateData> _climateData;

        public ClimateDataRepositoryStub(IReadOnlyList<ClimateData> climateData) => _climateData = climateData;

        public Task<ClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_climateData.FirstOrDefault(data =>
                climateZoneId == data.ClimateZoneId && month == data.Month));

        public Task<IReadOnlyList<int>> GetAvailableMonthsForClimateZoneAsync(
            int climateZoneId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>(
                _climateData
                    .Where(data => data.ClimateZoneId == climateZoneId)
                    .Select(data => data.Month)
                    .OrderBy(month => month)
                    .ToArray());
    }

    private sealed class EnergyPlusBenchmarkRunnerStub : IEnergyPlusBenchmarkRunner
    {
        public Task<Result<EnergyPlusBenchmarkResult>> RunAsync(
            EnergyPlusBenchmarkRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<EnergyPlusBenchmarkResult>.Success(new EnergyPlusBenchmarkResult
            {
                Succeeded = true,
                ExitCode = 0,
                RunArtifactId = request.RunName ?? "run-artifact",
                StandardOutput = "EnergyPlus validation completed.",
                StandardError = string.Empty
            }));
    }
}

