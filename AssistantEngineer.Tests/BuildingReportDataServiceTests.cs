using AssistantEngineer.Data;
using AssistantEngineer.Models;
using AssistantEngineer.Services.Calculations;
using AssistantEngineer.Services.Reports;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests;

public class BuildingReportDataServiceTests
{
    [Fact]
    public async Task BuildReportAsync_WhenBuildingDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.BuildReportAsync(buildingId: 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task BuildReportAsync_WithBuildingStructure_ReturnsCompleteReport()
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
            CreateRoom(id: 2, floorId: floor2.Id, areaM2: 20, heightM: 3, indoorTemperatureC: 24, outdoorTemperatureC: 34));
        context.Windows.Add(new Window { Id = 1, RoomId = 1, AreaM2 = 2 });
        context.Walls.AddRange(
            new Wall { Id = 1, RoomId = 1, AreaM2 = 10, IsExternal = true },
            new Wall { Id = 2, RoomId = 1, AreaM2 = 5, IsExternal = false });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.BuildReportAsync(building.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Project 1", result.ProjectName);
        Assert.Equal("Building 1", result.BuildingName);
        Assert.Equal(2, result.FloorsCount);
        Assert.Equal(2, result.RoomsCount);
        Assert.Equal(4730, result.TotalHeatLoadW);
        Assert.Equal(4.73, result.TotalHeatLoadKw);
        Assert.Equal(1.1, result.DesignReserveFactor);
        Assert.Equal(5203, result.DesignCapacityW);
        Assert.Equal(5.2, result.DesignCapacityKw);
        Assert.NotEqual(default, result.GeneratedAtUtc);

        Assert.Equal(2, result.FloorSummaries.Count);
        Assert.Equal(2330, result.FloorSummaries.Single(f => f.FloorId == floor1.Id).TotalHeatLoadW);
        Assert.Equal(2400, result.FloorSummaries.Single(f => f.FloorId == floor2.Id).TotalHeatLoadW);
        Assert.Equal(2563, result.FloorSummaries.Single(f => f.FloorId == floor1.Id).DesignCapacityW);
        Assert.Equal(2640, result.FloorSummaries.Single(f => f.FloorId == floor2.Id).DesignCapacityW);

        Assert.Equal(2, result.Rooms.Count);
        Assert.Equal(2330, result.Rooms.Single(r => r.RoomId == 1).TotalHeatLoadW);
        Assert.Equal(2400, result.Rooms.Single(r => r.RoomId == 2).TotalHeatLoadW);
        Assert.Equal(2563, result.Rooms.Single(r => r.RoomId == 1).DesignCapacityW);
        Assert.Equal(2640, result.Rooms.Single(r => r.RoomId == 2).DesignCapacityW);

        Assert.Single(result.Windows);
        Assert.Equal("Floor 1", result.Windows[0].FloorName);
        Assert.Equal("Room 1", result.Windows[0].RoomName);
        Assert.Equal(2, result.Windows[0].AreaM2);

        Assert.Equal(2, result.Walls.Count);
        Assert.Contains(result.Walls, wall => wall.IsExternal && wall.AreaM2 == 10);
        Assert.Contains(result.Walls, wall => !wall.IsExternal && wall.AreaM2 == 5);
    }

    private static BuildingReportDataService CreateService(AppDbContext context)
    {
        return new BuildingReportDataService(context, new RoomCalculationService());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
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
