using AssistantEngineer.Data;
using AssistantEngineer.Models;
using AssistantEngineer.Services;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests;

public class StructureCalculationServiceTests
{
    [Fact]
    public async Task CalculateFloorAsync_WhenFloorDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.CalculateFloorAsync(floorId: 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CalculateFloorAsync_WithRooms_ReturnsAggregatedFloorLoad()
    {
        // Arrange
        await using var context = CreateContext();
        var floor = await SeedStructureAsync(context);

        context.Rooms.AddRange(
            CreateRoom(id: 1, floorId: floor.Id, areaM2: 10, heightM: 3, indoorTemperatureC: 24, outdoorTemperatureC: 24, peopleCount: 1, equipmentLoadW: 70, lightingLoadW: 30),
            CreateRoom(id: 2, floorId: floor.Id, areaM2: 20, heightM: 3, indoorTemperatureC: 24, outdoorTemperatureC: 34));

        context.Windows.Add(new Window { Id = 1, RoomId = 1, AreaM2 = 2 });
        context.Walls.AddRange(
            new Wall { Id = 1, RoomId = 1, AreaM2 = 10, IsExternal = true },
            new Wall { Id = 2, RoomId = 1, AreaM2 = 5, IsExternal = false });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.CalculateFloorAsync(floor.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(floor.Id, result.FloorId);
        Assert.Equal("Floor 1", result.FloorName);
        Assert.Equal(2, result.RoomsCount);
        Assert.Equal(4730, result.TotalHeatLoadW);
        Assert.Equal(4.73, result.TotalHeatLoadKw);
    }

    [Fact]
    public async Task CalculateBuildingAsync_WhenBuildingDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.CalculateBuildingAsync(buildingId: 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CalculateBuildingAsync_WithFloorsAndRooms_ReturnsAggregatedBuildingLoad()
    {
        // Arrange
        await using var context = CreateContext();
        var project = new Project { Id = 1, Name = "Project 1" };
        var building = new Building { Id = 1, Name = "Building 1", ProjectId = project.Id };
        var floor1 = new Floor { Id = 1, Name = "Floor 1", BuildingId = building.Id };
        var floor2 = new Floor { Id = 2, Name = "Floor 2", BuildingId = building.Id };

        context.Projects.Add(project);
        context.Buildings.Add(building);
        context.Floors.AddRange(floor1, floor2);
        context.Rooms.AddRange(
            CreateRoom(id: 1, floorId: floor1.Id, areaM2: 10, heightM: 3, indoorTemperatureC: 24, outdoorTemperatureC: 24, peopleCount: 1, equipmentLoadW: 70, lightingLoadW: 30),
            CreateRoom(id: 2, floorId: floor2.Id, areaM2: 5, heightM: 3, indoorTemperatureC: 20, outdoorTemperatureC: 30));

        context.Windows.Add(new Window { Id = 1, RoomId = 1, AreaM2 = 2 });
        context.Walls.Add(new Wall { Id = 1, RoomId = 1, AreaM2 = 10, IsExternal = true });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.CalculateBuildingAsync(building.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(building.Id, result.BuildingId);
        Assert.Equal("Building 1", result.BuildingName);
        Assert.Equal(2, result.FloorsCount);
        Assert.Equal(2, result.RoomsCount);
        Assert.Equal(2930, result.TotalHeatLoadW);
        Assert.Equal(2.93, result.TotalHeatLoadKw);
    }

    private static StructureCalculationService CreateService(AppDbContext context)
    {
        return new StructureCalculationService(context, new RoomCalculationService());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Floor> SeedStructureAsync(AppDbContext context)
    {
        var project = new Project { Id = 1, Name = "Project 1" };
        var building = new Building { Id = 1, Name = "Building 1", ProjectId = project.Id };
        var floor = new Floor { Id = 1, Name = "Floor 1", BuildingId = building.Id };

        context.Projects.Add(project);
        context.Buildings.Add(building);
        context.Floors.Add(floor);
        await context.SaveChangesAsync();

        return floor;
    }

    private static Room CreateRoom(
        int id,
        int floorId,
        double areaM2,
        double heightM,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        int peopleCount = 0,
        double equipmentLoadW = 0,
        double lightingLoadW = 0)
    {
        return new Room
        {
            Id = id,
            Name = $"Room {id}",
            FloorId = floorId,
            AreaM2 = areaM2,
            HeightM = heightM,
            VolumeM3 = areaM2 * heightM,
            IndoorTemperatureC = indoorTemperatureC,
            OutdoorTemperatureC = outdoorTemperatureC,
            PeopleCount = peopleCount,
            EquipmentLoadW = equipmentLoadW,
            LightingLoadW = lightingLoadW
        };
    }
}
