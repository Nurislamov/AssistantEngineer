using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class HeatingLoadValidationTests
{
    [Fact]
    public async Task BuildingHeatingLoadReturnsValidationWhenClimateZoneIsMissing()
    {
        var project = DomainInvariantTests.CreateProject();
        var building = DomainInvariantTests.CreateBuilding(project);
        var floor = building.AddFloor("Level 1").Value;
        _ = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(-15).Value).Value;

        var facade = CreateLoadCalculationsFacade(building);

        var result = await facade.CalculateBuildingHeatingLoadAsync(
            building.Id,
            HeatingLoadCalculationMethodDto.En12831,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("climate zone", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RoomHeatingLoadReturnsValidationWhenClimateZoneIsMissing()
    {
        var floor = DomainInvariantTests.CreateFloor();
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(-15).Value).Value;

        var facade = CreateLoadCalculationsFacade(room.Floor.Building);

        var result = await facade.CalculateRoomHeatingLoadAsync(
            room.Id,
            HeatingLoadCalculationMethodDto.En12831,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("climate zone", result.Error, StringComparison.OrdinalIgnoreCase);
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
