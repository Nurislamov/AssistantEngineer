using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Services.Buildings;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Application.Services.Rooms;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;

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

        var service = new BuildingHeatingLoadService(
            new BuildingRepositoryStub(building),
            new EmptyPreferencesRepository(),
            CalculationTestFactory.CreateHeatingLoadCalculator());

        var result = await service.CalculateAsync(building.Id);

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

        var service = new RoomQueryService(
            new RoomRepositoryStub(room),
            new EmptyPreferencesRepository(),
            CalculationTestFactory.CreateRoomCoolingLoadCalculator(),
            CalculationTestFactory.CreateHeatingLoadCalculator(),
            CalculationTestFactory.CreateIso52016ClimateDataValidator());

        var result = await service.CalculateHeatingLoadAsync(room.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("climate zone", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building _building;

        public BuildingRepositoryStub(Building building) => _building = building;

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Building building) => throw new NotSupportedException();
    }

    private sealed class EmptyPreferencesRepository : ICalculationPreferencesRepository
    {
        public Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);
    }

    private sealed class RoomRepositoryStub : IRoomRepository
    {
        private readonly Room _room;

        public RoomRepositoryStub(Room room) => _room = room;

        public Task<Room?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(id == _room.Id ? _room : null);

        public Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Room room) => throw new NotSupportedException();
    }

}


