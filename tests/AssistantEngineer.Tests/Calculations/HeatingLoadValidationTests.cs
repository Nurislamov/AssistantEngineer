using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

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
            new BuildingHeatingReadModelRepositoryStub(building),
            new BuildingHeatingReadModelCalculator(
                Microsoft.Extensions.Options.Options.Create(new En12831HeatingLoadOptions())));

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

        var service = new RoomCalculationService(
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
                        null,
                        [],
                        []))
                    .ToList());
        }

        public Task<BuildingHeatingReadModel?> GetByIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BuildingHeatingReadModel?>(buildingId == _building.BuildingId ? _building : null);
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

        public Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(id == _room.Id ? _room : null);

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Room>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Room room) => throw new NotSupportedException();

        public void Remove(Room room) => throw new NotSupportedException();

        public void RemoveWindow(Window window) => throw new NotSupportedException();

        public void RemoveWall(Wall wall) => throw new NotSupportedException();
    }

}
