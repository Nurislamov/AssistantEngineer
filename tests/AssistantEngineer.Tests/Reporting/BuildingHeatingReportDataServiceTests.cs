using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.Modules.Reporting.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class BuildingHeatingReportDataServiceTests
{
    private static readonly DateTimeOffset FixedReportTime = new(2026, 4, 19, 8, 30, 0, TimeSpan.Zero);

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
        Assert.Contains("Standard-Based Calculation", result.Value.CalculationMethod, StringComparison.Ordinal);
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

    private static BuildingHeatingReportDataService CreateService(
        Building building)
    {
        var loadCalculations = CreateLoadCalculationsFacade(building);

        var calculationService = new BuildingHeatingReportCalculationService(
            loadCalculations);

        var reportGenerator = new BuildingHeatingReportGenerator(
            new FixedTimeProvider(FixedReportTime));

        return new BuildingHeatingReportDataService(
            calculationService,
            reportGenerator);
    }

    private static LoadCalculationsFacade CreateLoadCalculationsFacade(
        Building building)
    {
        var repository = new BuildingGraphRepositoryStub(building);
        var timeProvider = TimeProvider.System;
        var equipmentSizingUseCase = new EquipmentSizingCalculationUseCase(
            repository,
            repository,
            new RoomLoadCalculationEngine(timeProvider: timeProvider),
            new EquipmentSizingEngine(timeProvider),
            new CoolingLoadReferenceData(),
            Options.Create(new CoolingLoadCalculationOptions()),
            Options.Create(new En12831HeatingLoadOptions()));
        var systemEnergyUseCase = new SystemEnergyHandoffUseCase(
            new UnsupportedUsefulDemandProvider());
        var pipeline = new EnergyCalculationPipelineService(
            repository,
            repository,
            repository,
            repository,
            new RoomLoadCalculationEngine(timeProvider: timeProvider),
            new LoadAggregationEngine(timeProvider),
            new AnnualEnergyBalanceEngine(timeProvider),
            equipmentSizingUseCase,
            systemEnergyUseCase,
            new UnsupportedBuildingEnergyCalculator(),
            new CoolingLoadReferenceData(),
            Options.Create(new CoolingLoadCalculationOptions()),
            Options.Create(new En12831HeatingLoadOptions()),
            timeProvider);

        return new LoadCalculationsFacade(pipeline);
    }

    private static double WeightedAverage(
        IReadOnlyCollection<RoomHeatingLoadResult> rooms,
        Func<RoomHeatingLoadResult, double> valueSelector)
    {
        var totalWeight = rooms.Sum(room => room.TotalDesignHeatingLoadW);
        return rooms.Sum(room => valueSelector(room) * room.TotalDesignHeatingLoadW) / totalWeight;
    }

    private sealed class BuildingGraphRepositoryStub :
        IRoomRepository,
        IFloorRepository,
        IBuildingRepository,
        ICalculationPreferencesRepository
    {
        private readonly Building _building;

        public BuildingGraphRepositoryStub(Building building)
        {
            _building = building;
        }

        Task<Room?> IRoomRepository.GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindRoom(id));

        Task<Room?> IRoomRepository.GetForCalculationAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Floors.SelectMany(floor => floor.Rooms).ToList());

        Task<IReadOnlyList<Room>> IRoomRepository.ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Id == buildingId
                ? _building.Floors.SelectMany(floor => floor.Rooms).ToList()
                : []);

        public Task<IReadOnlyList<Room>> ListWithEngineeringInputsByBuildingIdAsync(
            int buildingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Id == buildingId
                ? _building.Floors.SelectMany(floor => floor.Rooms).ToList()
                : []);

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id) is not null);

        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Window>>(FindRoom(roomId)?.Windows.ToList() ?? []);

        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Wall>>(FindRoom(roomId)?.Walls.ToList() ?? []);

        public void Add(Room room) => throw new NotSupportedException();
        public void Remove(Room room) => throw new NotSupportedException();
        public void RemoveWindow(Window window) => throw new NotSupportedException();
        public void RemoveWall(Wall wall) => throw new NotSupportedException();

        Task<Floor?> IFloorRepository.GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_building.Floors.FirstOrDefault(floor => floor.Id == id));

        public Task<Floor?> GetWithRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_building.Floors.FirstOrDefault(floor => floor.Id == id));

        Task<Floor?> IFloorRepository.GetForCalculationAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_building.Floors.FirstOrDefault(floor => floor.Id == id));

        Task<IReadOnlyList<Floor>> IFloorRepository.ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Floor>>(_building.Id == buildingId ? _building.Floors.ToList() : []);

        public void Add(Floor floor) => throw new NotSupportedException();
        public void Remove(Floor floor) => throw new NotSupportedException();

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.ThermalZones.Any(zone => zone.Id == thermalZoneId) ? _building : null);

        Task<Building?> IBuildingRepository.GetForCalculationAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>(_building.ProjectId == projectId ? [_building] : []);

        public void Add(Building building) => throw new NotSupportedException();
        public void Remove(Building building) => throw new NotSupportedException();

        public Task<Building?> GetForValidationAsync(
            int id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);

        private Room? FindRoom(int id) =>
            _building.Floors.SelectMany(floor => floor.Rooms).FirstOrDefault(room => room.Id == id);
    }

    private sealed class UnsupportedBuildingEnergyCalculator : IBuildingEnergyCalculator
    {
        public Task<BuildingEnergyBalanceResult> CalculateAsync(
            Building building,
            CoolingLoadCalculationMethod coolingMethod,
            HeatingLoadCalculationMethod heatingMethod,
            CalculationPreferences? preferences = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class UnsupportedUsefulDemandProvider : ISystemEnergyHandoffUsefulDemandProvider
    {
        public Task<Result<BuildingEnergyBalanceResult>> CalculateUsefulDemandAsync(
            int buildingId,
            CoolingLoadCalculationMethod coolingMethod,
            HeatingLoadCalculationMethod heatingMethod,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
