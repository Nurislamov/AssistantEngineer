using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class EngineeringWorkflowBulkInputQueryServiceTests
{
    [Fact]
    public async Task GetByBuildingIdAsync_UsesSingleBulkCallAndReturnsDeterministicOrdering()
    {
        var graph = CreateGraph();
        var repository = new RoomRepositoryStub([graph.RoomPrimary, graph.RoomSecondary]);
        var service = new EngineeringWorkflowBulkInputQueryService(repository);

        var result = await service.GetByBuildingIdAsync(graph.BuildingId, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, repository.ListWithEngineeringInputsCalls);

        Assert.Equal([10, 20], result.Value.Rooms.Select(room => room.RoomId).ToArray());
        Assert.Equal([10, 20], result.Value.Walls.Select(wall => wall.RoomId).Distinct().ToArray());
        Assert.Equal([7, 2, 4], result.Value.Walls.Select(wall => wall.Id).ToArray());
        Assert.Equal([10, 20], result.Value.Windows.Select(window => window.RoomId).Distinct().ToArray());
        Assert.Equal([3, 9], result.Value.Windows.Select(window => window.Id).ToArray());

        Assert.Equal(1, result.Value.VentilationConfiguredRoomCount);
        Assert.Equal(1, result.Value.GroundConfiguredRoomCount);

        var secondary = Assert.Single(result.Value.Rooms, room => room.RoomId == 10);
        Assert.True(secondary.HasVentilationParameters);
        Assert.False(secondary.HasGroundContactMetadata);

        var primary = Assert.Single(result.Value.Rooms, room => room.RoomId == 20);
        Assert.False(primary.HasVentilationParameters);
        Assert.True(primary.HasGroundContactMetadata);
    }

    [Fact]
    public async Task GetByBuildingIdAsync_PreservesMissingOptionalInputsWithoutFailure()
    {
        var graph = CreateGraph();
        var room = graph.RoomSecondary;
        room.SetVentilationParameters(null);
        var repository = new RoomRepositoryStub([room]);
        var service = new EngineeringWorkflowBulkInputQueryService(repository);

        var result = await service.GetByBuildingIdAsync(graph.BuildingId, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, repository.ListWithEngineeringInputsCalls);
        Assert.Equal(0, result.Value.GroundConfiguredRoomCount);
        Assert.Equal(0, result.Value.VentilationConfiguredRoomCount);
        Assert.Single(result.Value.Rooms);
        Assert.False(result.Value.Rooms[0].HasGroundContactMetadata);
        Assert.False(result.Value.Rooms[0].HasVentilationParameters);
    }

    private static (int BuildingId, Room RoomPrimary, Room RoomSecondary) CreateGraph()
    {
        var project = DomainInvariantTests.CreateProject("Bulk project");
        var building = DomainInvariantTests.CreateBuilding(project, "Bulk building");
        SetEntityId(building, 101);

        var floor = building.AddFloor("Ground floor").Value;
        SetEntityId(floor, 201);
        SetEntityBackingField(floor, "<BuildingId>k__BackingField", 101);

        var roomPrimary = floor.AddRoom(
            "Room-Primary",
            Area.FromSquareMeters(32).Value,
            3.1,
            Temperature.FromCelsius(21).Value,
            Temperature.FromCelsius(-8).Value).Value;
        var roomSecondary = floor.AddRoom(
            "Room-Secondary",
            Area.FromSquareMeters(28).Value,
            3.0,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(-8).Value).Value;

        SetEntityId(roomPrimary, 20);
        SetEntityId(roomSecondary, 10);

        var primaryWallEast = roomPrimary.AddWall(
            Area.FromSquareMeters(12).Value,
            ThermalTransmittance.FromValue(0.9).Value,
            CardinalDirection.East,
            WallBoundaryType.External).Value;
        var primaryWallNorth = roomPrimary.AddWall(
            Area.FromSquareMeters(11).Value,
            ThermalTransmittance.FromValue(0.85).Value,
            CardinalDirection.North,
            WallBoundaryType.External).Value;
        var secondaryWallEast = roomSecondary.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.95).Value,
            CardinalDirection.East,
            WallBoundaryType.External).Value;

        SetEntityId(primaryWallEast, 4);
        SetEntityId(primaryWallNorth, 2);
        SetEntityId(secondaryWallEast, 7);

        var primaryWindowEast = roomPrimary.AddWindow(
            Area.FromSquareMeters(2.5).Value,
            ThermalTransmittance.FromValue(1.3).Value,
            SolarHeatGainCoefficient.FromValue(0.45).Value,
            CardinalDirection.East).Value;
        var secondaryWindowEast = roomSecondary.AddWindow(
            Area.FromSquareMeters(2.0).Value,
            ThermalTransmittance.FromValue(1.4).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.East).Value;

        SetEntityId(primaryWindowEast, 9);
        SetEntityId(secondaryWindowEast, 3);

        var ventilation = VentilationParameters.Create(
            airChangesPerHour: 0.6,
            heatRecoveryEfficiency: 0.5).Value;
        roomSecondary.SetVentilationParameters(ventilation);

        var ground = GroundContactMetadata.Create(
            GroundContactType.SlabOnGround,
            exposedPerimeterM: 18,
            burialDepthM: 0.2,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 0,
            perimeterInsulationDepthM: 0,
            underfloorVentilationAirChangesPerHour: 0).Value;
        roomPrimary.SetGroundContactMetadata(ground);

        return (101, roomPrimary, roomSecondary);
    }

    private static void SetEntityId(object entity, int id)
    {
        SetEntityBackingField(entity, "<Id>k__BackingField", id);
    }

    private static void SetEntityBackingField(object entity, string fieldName, int value)
    {
        var field = entity.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, value);
    }

    private sealed class RoomRepositoryStub : IRoomRepository
    {
        private readonly IReadOnlyList<Room> _rooms;

        public RoomRepositoryStub(IReadOnlyList<Room> rooms)
        {
            _rooms = rooms;
        }

        public int ListWithEngineeringInputsCalls { get; private set; }

        public Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Room?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Room>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Room>> ListWithEngineeringInputsByBuildingIdAsync(
            int buildingId,
            CancellationToken cancellationToken = default)
        {
            ListWithEngineeringInputsCalls++;
            return Task.FromResult<IReadOnlyList<Room>>(_rooms.Reverse().ToArray());
        }

        public void Add(Room room) => throw new NotSupportedException();
        public void Remove(Room room) => throw new NotSupportedException();
        public void RemoveWindow(Window window) => throw new NotSupportedException();
        public void RemoveWall(Wall wall) => throw new NotSupportedException();
    }
}
