using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Floors;
using AssistantEngineer.Modules.Buildings.Application.Services.Projects;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Services;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class CrudUseCaseTests
{
    [Fact]
    public async Task ProjectBuildingFloorAndRoomCommandsUpdateAndDelete()
    {
        var graph = CreateGraph();
        var unitOfWork = new CountingUnitOfWork();
        var projectRepository = new ProjectRepositoryStub([graph.Project]);
        var buildingRepository = new BuildingRepositoryStub(graph.Building);
        var floorRepository = new FloorRepositoryStub(graph.Floor);
        var roomRepository = new RoomRepositoryStub(graph.Room, graph.Floor);

        var projectCommand = new ProjectCommandService(projectRepository, unitOfWork);
        var buildingCommand = new BuildingCommandService(
            projectRepository,
            new ClimateZoneRepositoryStub(),
            buildingRepository,
            unitOfWork);
        var floorCommand = new FloorCommandService(buildingRepository, floorRepository, unitOfWork);
        var roomCommand = new RoomCommandService(floorRepository, roomRepository, unitOfWork);

        var projectResult = await projectCommand.UpdateAsync(
            graph.Project.Id,
            new UpdateProjectRequest { Name = "Updated project" });
        var buildingResult = await buildingCommand.UpdateAsync(
            graph.Building.Id,
            new UpdateBuildingRequest { Name = "Updated building" });
        var floorResult = await floorCommand.UpdateAsync(
            graph.Floor.Id,
            new UpdateFloorRequest { Name = "Updated floor" });
        var roomResult = await roomCommand.UpdateAsync(
            graph.Room.Id,
            new UpdateRoomRequest
            {
                Name = "Updated room",
                AreaM2 = 25,
                HeightM = 3.2,
                IndoorTemperatureC = 21,
                OutdoorTemperatureOverrideC = -5,
                PeopleCount = 3,
                EquipmentLoadW = 500,
                LightingLoadW = 250,
                Type = RoomTypeDto.MeetingRoom
            });

        Assert.True(projectResult.IsSuccess, projectResult.Error);
        Assert.True(buildingResult.IsSuccess, buildingResult.Error);
        Assert.True(floorResult.IsSuccess, floorResult.Error);
        Assert.True(roomResult.IsSuccess, roomResult.Error);
        Assert.Equal("Updated project", graph.Project.Name);
        Assert.Equal("Updated building", graph.Building.Name);
        Assert.Equal("Updated floor", graph.Floor.Name);
        Assert.Equal("Updated room", graph.Room.Name);
        Assert.Equal(25, graph.Room.Area.SquareMeters);
        Assert.Equal(RoomType.MeetingRoom, graph.Room.Type);

        Assert.True((await projectCommand.DeleteAsync(graph.Project.Id)).IsSuccess);
        Assert.True((await buildingCommand.DeleteAsync(graph.Building.Id)).IsSuccess);
        Assert.True((await floorCommand.DeleteAsync(graph.Floor.Id)).IsSuccess);
        Assert.True((await roomCommand.DeleteAsync(graph.Room.Id)).IsSuccess);
        Assert.True(projectRepository.Removed);
        Assert.True(buildingRepository.Removed);
        Assert.True(floorRepository.Removed);
        Assert.True(roomRepository.Removed);
        Assert.True(unitOfWork.SaveCount >= 8);
    }

    [Fact]
    public async Task RoomEnvelopeCommandsUpdateAndDeleteWallsAndWindows()
    {
        var graph = CreateGraph();
        var unitOfWork = new CountingUnitOfWork();
        var floorRepository = new FloorRepositoryStub(graph.Floor);
        var roomRepository = new RoomRepositoryStub(graph.Room, graph.Floor);
        var roomCommand = new RoomCommandService(floorRepository, roomRepository, unitOfWork);

        var wall = graph.Room.Walls.Single();
        var window = graph.Room.Windows.Single();

        var wallResult = await roomCommand.UpdateWallAsync(
            graph.Room.Id,
            wall.Id,
            new UpdateWallRequest
            {
                AreaM2 = 18,
                UValue = 0.8,
                Orientation = CardinalDirectionDto.East,
                BoundaryType = WallBoundaryTypeDto.External
            });
        var windowResult = await roomCommand.UpdateWindowAsync(
            graph.Room.Id,
            window.Id,
            new UpdateWindowRequest
            {
                AreaM2 = 4,
                UValue = 1.4,
                Shgc = 0.45,
                Orientation = CardinalDirectionDto.East
            });

        Assert.True(wallResult.IsSuccess, wallResult.Error);
        Assert.True(windowResult.IsSuccess, windowResult.Error);
        Assert.Equal(18, wall.Area.SquareMeters);
        Assert.Equal(0.8, wall.UValue.Value);
        Assert.Equal(CardinalDirection.East, wall.Orientation);
        Assert.Equal(4, window.Area.SquareMeters);
        Assert.Equal(1.4, window.UValue.Value);
        Assert.Equal(0.45, window.Shgc.Value);

        Assert.True((await roomCommand.DeleteWindowAsync(graph.Room.Id, window.Id)).IsSuccess);
        Assert.True((await roomCommand.DeleteWallAsync(graph.Room.Id, wall.Id)).IsSuccess);
        Assert.True(roomRepository.RemovedWindow);
        Assert.True(roomRepository.RemovedWall);
    }

    [Fact]
    public async Task EquipmentCatalogDeleteDeactivatesItem()
    {
        var item = CoolingEquipmentCatalogItem.Create(
            "Aero",
            "Split",
            "Wall",
            "Aero 25",
            Power.FromWatts(2500).Value).Value;
        SetEntityId(item, 10);
        var repository = new EquipmentCatalogRepositoryStub(item);
        var unitOfWork = new CountingUnitOfWork();
        var command = new CoolingEquipmentCatalogCommandService(repository, unitOfWork);

        var updateResult = await command.UpdateAsync(
            item.Id,
            new UpdateEquipmentCatalogItemRequest
            {
                Manufacturer = "Aero",
                SystemType = "VRF",
                UnitType = "Cassette",
                ModelName = "Aero 50",
                NominalCoolingCapacityKw = 5,
                IsActive = true
            });
        var deleteResult = await command.DeactivateAsync(item.Id);

        Assert.True(updateResult.IsSuccess, updateResult.Error);
        Assert.Equal("VRF", item.SystemType);
        Assert.Equal(5, item.NominalCoolingCapacity.Kilowatts);
        Assert.True(deleteResult.IsSuccess, deleteResult.Error);
        Assert.False(item.IsActive);
        Assert.Equal(2, unitOfWork.SaveCount);
    }

    private static BuildingGraph CreateGraph()
    {
        var project = DomainInvariantTests.CreateProject("Project");
        SetEntityId(project, 1);

        var building = DomainInvariantTests.CreateBuilding(project, "Building");
        SetEntityId(building, 2);
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Floor").Value;
        SetEntityId(floor, 3);

        var room = floor.AddRoom(
            "Room",
            Area.FromSquareMeters(30).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(30).Value,
            peopleCount: 1,
            equipmentLoad: Power.FromWatts(100).Value,
            lightingLoad: Power.FromWatts(50).Value).Value;
        SetEntityId(room, 4);

        var wall = room.AddWall(
            Area.FromSquareMeters(20).Value,
            ThermalTransmittance.FromValue(1).Value,
            CardinalDirection.East,
            WallBoundaryType.External).Value;
        SetEntityId(wall, 5);

        var window = room.AddWindow(
            Area.FromSquareMeters(3).Value,
            ThermalTransmittance.FromValue(2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.East).Value;
        SetEntityId(window, 6);

        return new BuildingGraph(project, building, floor, room);
    }

    private static void SetEntityId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }

    private sealed record BuildingGraph(Project Project, Building Building, Floor Floor, Room Room);

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class ProjectRepositoryStub : IProjectRepository
    {
        private readonly List<Project> _projects;

        public ProjectRepositoryStub(IEnumerable<Project> projects) => _projects = projects.ToList();

        public bool Removed { get; private set; }

        public Task<Project?> GetByIdAsync(int id, bool includeBuildings = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_projects.FirstOrDefault(project => project.Id == id));

        public Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>(_projects);

        public void Add(Project project) => _projects.Add(project);

        public void Remove(Project project)
        {
            Removed = true;
            _projects.Remove(project);
        }
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building _building;

        public BuildingRepositoryStub(Building building) => _building = building;

        public bool Removed { get; private set; }

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.ThermalZones.Any(zone => zone.Id == thermalZoneId) ? _building : null);

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>(_building.ProjectId == projectId ? [_building] : []);

        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.Id == id ? _building : null);

        public void Add(Building building) => throw new NotSupportedException();

        public void Remove(Building building) => Removed = true;
    }

    private sealed class FloorRepositoryStub : IFloorRepository
    {
        private readonly Floor _floor;

        public FloorRepositoryStub(Floor floor) => _floor = floor;

        public bool Removed { get; private set; }

        public Task<Floor?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Floor?>(_floor.Id == id ? _floor : null);

        public Task<Floor?> GetWithRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Floor?>(_floor.Id == id ? _floor : null);

        public Task<Floor?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Floor?>(_floor.Id == id ? _floor : null);

        public Task<IReadOnlyList<Floor>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Floor>>(_floor.BuildingId == buildingId ? [_floor] : []);

        public void Add(Floor floor) => throw new NotSupportedException();

        public void Remove(Floor floor) => Removed = true;
    }

    private sealed class RoomRepositoryStub : IRoomRepository
    {
        private readonly Room _room;
        private readonly Floor _floor;

        public RoomRepositoryStub(Room room, Floor floor)
        {
            _room = room;
            _floor = floor;
        }

        public bool Removed { get; private set; }
        public bool RemovedWindow { get; private set; }
        public bool RemovedWall { get; private set; }

        public Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room.Id == id ? _room : null);

        public Task<Room?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room.Id == id ? _room : null);

        public Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room.Id == id ? _room : null);

        public Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room.Id == id ? _room : null);

        public Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room.Id == id ? _room : null);

        public Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room.Id == id ? _room : null);

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>([_room]);

        public Task<IReadOnlyList<Room>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_floor.BuildingId == buildingId ? [_room] : []);

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_room.Id == id);

        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Window>>(_room.Id == roomId ? _room.Windows.ToArray() : []);

        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Wall>>(_room.Id == roomId ? _room.Walls.ToArray() : []);

        public void Add(Room room) => throw new NotSupportedException();

        public void Remove(Room room) => Removed = true;

        public void RemoveWindow(Window window) => RemovedWindow = true;

        public void RemoveWall(Wall wall) => RemovedWall = true;
    }

    private sealed class ClimateZoneRepositoryStub : IClimateZoneRepository
    {
        public Task<ClimateZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ClimateZone?>(null);
    }

    private sealed class EquipmentCatalogRepositoryStub : IEquipmentCatalogRepository
    {
        private readonly CoolingEquipmentCatalogItem _item;

        public EquipmentCatalogRepositoryStub(CoolingEquipmentCatalogItem item) => _item = item;

        public Task<CoolingEquipmentCatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<CoolingEquipmentCatalogItem?>(_item.Id == id ? _item : null);

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CoolingEquipmentCatalogItem>>([_item]);

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListActiveByTypeAsync(
            string systemType,
            string unitType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CoolingEquipmentCatalogItem>>(
                _item.IsActive && _item.SystemType == systemType && _item.UnitType == unitType ? [_item] : []);

        public void Add(CoolingEquipmentCatalogItem item) => throw new NotSupportedException();
    }
}
