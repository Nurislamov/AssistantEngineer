using System.Net.Http.Json;
using System.Net;
using System.Reflection;
using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Abstractions.Repositories;
using AssistantEngineer.Application.Contracts.Benchmarks;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Contracts.Reports;
using AssistantEngineer.Application.Services.Benchmarks;
using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Models.Climate;
using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;
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
    public async Task GetHeatingReportReturnsReport()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reports/buildings/0/heating?method=En12831");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<HeatingReport>();
        Assert.NotNull(report);
        Assert.Equal("Integration project", report.ProjectName);
        Assert.Equal("Integration building", report.BuildingName);
        Assert.Equal(1, report.RoomsCount);
        Assert.True(report.TotalDesignHeatingLoadW > 0);
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
    }

    [Fact]
    public async Task GetEnergyBalanceReturnsAnnualBalance()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/buildings/0/energy-balance?coolingMethod=Simplified&heatingMethod=En12831");

        response.EnsureSuccessStatusCode();
        var balance = await response.Content.ReadFromJsonAsync<BuildingEnergyBalanceResult>();
        Assert.NotNull(balance);
        Assert.Equal("Integration building", balance.BuildingName);
        Assert.NotEmpty(balance.MonthlyBalances);
        Assert.True(balance.AnnualTotalDemandKWh > 0);
    }

    [Fact]
    public async Task GetIso52016BuildingCalculationReturnsThermalZoneBreakdown()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/buildings/0/calculate?method=Iso52016");

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

        var response = await client.GetAsync("/api/buildings/0/calculate?method=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Contains("method", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetEnergyBalanceExcelReturnsWorkbook()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/reports/buildings/0/energy-balance/excel?coolingMethod=Simplified&heatingMethod=En12831");

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
            ModelPath = "fake-model.idf",
            WeatherFilePath = "fake-weather.epw",
            OutputDirectory = "fake-output"
        };

        var response = await client.PostAsJsonAsync("/api/benchmarks/energyplus", request);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EnergyPlusBenchmarkResult>();
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(request.OutputDirectory, result.OutputDirectory);
    }

    [Fact]
    public async Task PostEnergyPlusModelExportCreatesIdfFile()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            await using var factory = new AssistantEngineerApiFactory();
            var client = factory.CreateClient();
            var outputPath = Path.Combine(tempDirectory, "integration-building.idf");

            var response = await client.PostAsJsonAsync(
                "/api/benchmarks/energyplus/buildings/0/model",
                new EnergyPlusModelExportRequest { OutputPath = outputPath });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EnergyPlusModelExportResult>();
            Assert.NotNull(result);
            Assert.Equal("Integration building", result.BuildingName);
            Assert.Equal(outputPath, result.ModelPath);
            Assert.True(File.Exists(outputPath));

            var idf = await File.ReadAllTextAsync(outputPath);
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

        var response = await client.GetAsync("/api/benchmarks/iso52016/reference-cases");

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
    public async Task PostProjectWithInvalidBodyReturnsValidationProblem()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Contains("Name", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetReportForUnknownBuildingReturnsNotFound()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reports/buildings/999?method=Simplified");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Not found", problem.Title);
    }

    [Fact]
    public async Task GetCoolingReportWithPartialEquipmentSelectionReturnsBadRequest()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reports/buildings/0?method=Simplified&systemType=Split");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Both systemType and unitType", problem.Detail);
    }

    private sealed class AssistantEngineerApiFactory : WebApplicationFactory<Program>
    {
        private readonly Building _building;
        private readonly IReadOnlyList<ClimateData> _climateData;

        public AssistantEngineerApiFactory()
        {
            var project = DomainInvariantTests.CreateProject("Integration project");
            var climateZone = ClimateZone.Create(
                "Integration climate",
                Temperature.FromCelsius(35).Value,
                Temperature.FromCelsius(-12).Value).Value;
            _building = Building.Create("Integration building", project, climateZone).Value;
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
            Assert.True(_building.AddThermalZone("Office zone", [room.Id]).IsSuccess);
            Assert.True(room.AddWindow(
                Area.FromSquareMeters(3).Value,
                ThermalTransmittance.FromValue(2).Value,
                SolarHeatGainCoefficient.FromValue(0.5).Value,
                CardinalDirection.South).IsSuccess);

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
                services.RemoveAll<ICalculationPreferencesRepository>();
                services.RemoveAll<IEquipmentCatalogRepository>();
                services.RemoveAll<IClimateDataRepository>();
                services.RemoveAll<IEnergyPlusBenchmarkRunner>();

                services.AddScoped<IBuildingRepository>(_ => new BuildingRepositoryStub(_building));
                services.AddScoped<ICalculationPreferencesRepository, EmptyPreferencesRepository>();
                services.AddScoped<IEquipmentCatalogRepository, EmptyEquipmentCatalogRepository>();
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
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"assistant-engineer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building _building;

        public BuildingRepositoryStub(Building building) => _building = building;

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>([_building]);

        public void Add(Building building) => throw new NotSupportedException();
    }

    private sealed class EmptyPreferencesRepository : ICalculationPreferencesRepository
    {
        public Task<CalculationPreferences?> GetByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);
    }

    private sealed class EmptyEquipmentCatalogRepository : IEquipmentCatalogRepository
    {
        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListActiveByTypeAsync(
            string systemType,
            string unitType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CoolingEquipmentCatalogItem>>([]);

        public Task<CoolingEquipmentCatalogItem?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CoolingEquipmentCatalogItem?>(null);

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CoolingEquipmentCatalogItem>>([]);

        public void Add(CoolingEquipmentCatalogItem item) => throw new NotSupportedException();
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
                OutputDirectory = request.OutputDirectory,
                StandardOutput = "EnergyPlus validation completed.",
                StandardError = string.Empty
            }));
    }
}


