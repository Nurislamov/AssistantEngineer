using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Equipment.Application.Services;
using AssistantEngineer.Modules.Reporting.Application.Services;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class BuildingReportDataServiceTests
{
    private static readonly DateTimeOffset FixedReportTime = new(2026, 4, 19, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task BuildReportAsyncReturnsPopulatedBuildingReport()
    {
        var project = DomainInvariantTests.CreateProject("Headquarters");
        var building = DomainInvariantTests.CreateBuilding(project, "Main");
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office 101",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value,
            peopleCount: 2).Value;
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.2).Value,
            CardinalDirection.South).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(3).Value,
            ThermalTransmittance.FromValue(2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.South).IsSuccess);

        var roomCalculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var service = CreateService(building, roomCalculator);

        var result = await service.BuildReportAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("Headquarters", result.Value.ProjectName);
        Assert.Equal("Main", result.Value.BuildingName);
        Assert.Equal(1, result.Value.FloorsCount);
        Assert.Equal(1, result.Value.RoomsCount);
        Assert.Single(result.Value.FloorSummaries);
        Assert.Single(result.Value.Rooms);
        Assert.Single(result.Value.Windows);
        Assert.Single(result.Value.Walls);
        Assert.False(result.Value.EquipmentSelectionRequested);
        Assert.Equal(FixedReportTime.UtcDateTime, result.Value.GeneratedAtUtc);
    }

    [Fact]
    public async Task BuildReportAsyncReturnsNotFoundWhenBuildingDoesNotExist()
    {
        var project = DomainInvariantTests.CreateProject("Headquarters");
        var building = DomainInvariantTests.CreateBuilding(project, "Main");
        var service = CreateService(building);

        var result = await service.BuildReportAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task BuildHeatingReportAsyncReturnsHeatingLoads()
    {
        var project = DomainInvariantTests.CreateProject("Headquarters");
        var climateZone = ClimateZone.Create(
            "Cold climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-15).Value).Value;
        var building = Building.Create("Main", project, climateZone).Value;
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office 101",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(5).Value).Value;
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.2).Value,
            CardinalDirection.South).IsSuccess);

        var service = CreateService(building);

        var result = await service.BuildHeatingReportAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("Headquarters", result.Value.ProjectName);
        Assert.Equal("Main", result.Value.BuildingName);
        Assert.Equal(nameof(HeatingLoadCalculationMethod.En12831), result.Value.CalculationMethod);
        Assert.Equal(1, result.Value.RoomsCount);
        Assert.Single(result.Value.Rooms);
        Assert.Equal(-15, result.Value.OutdoorDesignTemperatureC);
        Assert.True(result.Value.TotalDesignHeatingLoadW > 0);
        Assert.Equal(FixedReportTime.UtcDateTime, result.Value.GeneratedAtUtc);
    }

    [Fact]
    public async Task BuildHeatingReportAsyncSummarizesTemperaturesAcrossAllRooms()
    {
        var project = DomainInvariantTests.CreateProject("Headquarters");
        var climateZone = ClimateZone.Create(
            "Cold climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-15).Value).Value;
        var building = Building.Create("Main", project, climateZone).Value;
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Level 1").Value;
        var firstRoom = floor.AddRoom(
            "Office 101",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            Temperature.FromCelsius(-10).Value).Value;
        var secondRoom = floor.AddRoom(
            "Office 102",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(24).Value,
            Temperature.FromCelsius(-20).Value).Value;
        foreach (var room in new[] { firstRoom, secondRoom })
        {
            Assert.True(room.AddWall(
                Area.FromSquareMeters(12).Value,
                isExternal: true,
                ThermalTransmittance.FromValue(1.2).Value,
                CardinalDirection.South).IsSuccess);
        }

        var service = CreateService(building);

        var result = await service.BuildHeatingReportAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(2, result.Value.RoomsCount);
        Assert.Equal(
            WeightedAverage(result.Value.Rooms, room => room.IndoorDesignTemperatureC),
            result.Value.IndoorDesignTemperatureC,
            precision: 2);
        Assert.Equal(
            WeightedAverage(result.Value.Rooms, room => room.OutdoorDesignTemperatureC),
            result.Value.OutdoorDesignTemperatureC,
            precision: 2);
        Assert.NotEqual(result.Value.Rooms[0].IndoorDesignTemperatureC, result.Value.IndoorDesignTemperatureC);
        Assert.Equal(-15, result.Value.OutdoorDesignTemperatureC);
    }

    private static BuildingReportDataService CreateService(
        Building building,
        IRoomCoolingLoadCalculator? roomCalculator = null)
    {
        roomCalculator ??= CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var calculationService = new BuildingReportCalculationService(
            CalculationTestFactory.CreateAggregateCalculator(roomCalculator),
            roomCalculator,
            new CoolingEquipmentSelector(roomCalculator));
        var heatingLoadService = new BuildingHeatingLoadService(
            new BuildingHeatingReadModelRepositoryStub(building),
            new BuildingHeatingReadModelCalculator(
                Microsoft.Extensions.Options.Options.Create(new En12831HeatingLoadOptions())));
        var reportGenerator = new BuildingReportGenerator(new FixedTimeProvider(FixedReportTime));

        return new BuildingReportDataService(
            new BuildingRepositoryStub(building),
            new EmptyPreferencesRepository(),
            new EmptyEquipmentCatalogRepository(),
            CalculationTestFactory.CreateIso52016ClimateDataValidator(),
            calculationService,
            heatingLoadService,
            reportGenerator);
    }

    private static double WeightedAverage(
        IReadOnlyCollection<RoomHeatingLoadResult> rooms,
        Func<RoomHeatingLoadResult, double> valueSelector)
    {
        var totalWeight = rooms.Sum(room => room.TotalDesignHeatingLoadW);
        return rooms.Sum(room => valueSelector(room) * room.TotalDesignHeatingLoadW) / totalWeight;
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building _building;

        public BuildingRepositoryStub(Building building) => _building = building;

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.ThermalZones.Any(zone => zone.Id == thermalZoneId) ? _building : null);

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetForValidationAsync(
            int id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Building building) => throw new NotSupportedException();
    }

    private sealed class EmptyPreferencesRepository : ICalculationPreferencesRepository
    {
        public Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);
    }

    private sealed class EmptyEquipmentCatalogRepository : IEquipmentCatalogRepository
    {
        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListActiveByTypeAsync(
            string systemType,
            string unitType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CoolingEquipmentCatalogItem>>([]);

        public Task<CoolingEquipmentCatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
}
