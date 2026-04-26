using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class BuildingCoolingReportDataServiceTests
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

        var service = CreateService(building);

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

    private static BuildingCoolingReportDataService CreateService(
        Building building,
        IRoomCoolingLoadCalculator? roomCalculator = null)
    {
        roomCalculator ??= CalculationTestFactory.CreateRoomCoolingLoadCalculator();

        var loadCalculations = new CoolingLoadCalculationsFacadeStub(
            building,
            roomCalculator);

        var calculationService = new BuildingCoolingReportCalculationService(
            loadCalculations,
            new EquipmentFacadeStub());

        var reportGenerator = new BuildingCoolingReportGenerator(
            new FixedTimeProvider(FixedReportTime));

        return new BuildingCoolingReportDataService(
            new BuildingRepositoryStub(building),
            calculationService,
            reportGenerator);
    }

    private sealed class CoolingLoadCalculationsFacadeStub : ILoadCalculationsFacade
    {
        private readonly Building _building;
        private readonly IAggregateLoadCalculator _aggregateCalculator;
        private readonly IRoomCoolingLoadCalculator _roomCoolingLoadCalculator;

        public CoolingLoadCalculationsFacadeStub(
            Building building,
            IRoomCoolingLoadCalculator roomCoolingLoadCalculator)
        {
            _building = building;
            _roomCoolingLoadCalculator = roomCoolingLoadCalculator;
            _aggregateCalculator = CalculationTestFactory.CreateAggregateCalculator(roomCoolingLoadCalculator);
        }

        public async Task<Result<BuildingCalculationResult>> CalculateBuildingCoolingLoadAsync(
            int buildingId,
            CoolingLoadCalculationMethodDto method,
            CancellationToken cancellationToken)
        {
            if (buildingId != _building.Id)
                return Result<BuildingCalculationResult>.NotFound($"Building with id {buildingId} not found.");

            var result = await _aggregateCalculator.CalculateBuildingAsync(
                _building,
                method.ToDomain(),
                preferences: null,
                cancellationToken);

            return Result<BuildingCalculationResult>.Success(result);
        }

        public Task<Result<BuildingHeatingLoadResult>> CalculateBuildingHeatingLoadAsync(
            int buildingId,
            HeatingLoadCalculationMethodDto method,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<BuildingEnergyBalanceResult>> CalculateBuildingEnergyBalanceAsync(
            int buildingId,
            CoolingLoadCalculationMethodDto coolingMethod,
            HeatingLoadCalculationMethodDto heatingMethod,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public async Task<Result<FloorCalculationResult>> CalculateFloorCoolingLoadAsync(
            int floorId,
            CoolingLoadCalculationMethodDto method,
            CancellationToken cancellationToken)
        {
            var floor = _building.Floors.FirstOrDefault(floor => floor.Id == floorId);

            if (floor is null)
                return Result<FloorCalculationResult>.NotFound($"Floor with id {floorId} not found.");

            var result = await _aggregateCalculator.CalculateFloorAsync(
                floor,
                method.ToDomain(),
                preferences: null,
                cancellationToken);

            return Result<FloorCalculationResult>.Success(result);
        }

        public async Task<Result<RoomCalculationResult>> CalculateRoomCoolingLoadAsync(
            int roomId,
            CoolingLoadCalculationMethodDto method,
            CancellationToken cancellationToken)
        {
            var room = _building.Floors
                .SelectMany(floor => floor.Rooms)
                .FirstOrDefault(room => room.Id == roomId);

            if (room is null)
                return Result<RoomCalculationResult>.NotFound($"Room with id {roomId} not found.");

            var result = await _roomCoolingLoadCalculator.CalculateAsync(
                room,
                method.ToDomain(),
                preferences: null,
                cancellationToken);

            return Result<RoomCalculationResult>.Success(result);
        }

        public Task<Result<RoomHeatingLoadResult>> CalculateRoomHeatingLoadAsync(
            int roomId,
            HeatingLoadCalculationMethodDto method,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class EquipmentFacadeStub : IEquipmentFacade
    {
        public Task<Result<EquipmentCatalogItemResponse>> CreateCatalogItemAsync(
            CreateEquipmentCatalogItemRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<EquipmentCatalogItemResponse>> GetCatalogItemByIdAsync(
            int id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<List<EquipmentCatalogItemResponse>>> GetCatalogItemsAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<EquipmentSelectionResult>> SelectRoomEquipmentAsync(
            int roomId,
            EquipmentSelectionRequest request,
            double totalHeatLoadKw,
            double designCapacityKw,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result<EquipmentSelectionResult>.Failure(
                "Equipment selection is not expected in this test."));
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building _building;

        public BuildingRepositoryStub(
            Building building)
        {
            _building = building;
        }

        public Task<Building?> GetForReportAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithFloorsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(
            int thermalZoneId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.ThermalZones.Any(zone => zone.Id == thermalZoneId) ? _building : null);

        public Task<Building?> GetForCalculationAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetForValidationAsync(
            int id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(
            Building building) =>
            throw new NotSupportedException();
    }
}