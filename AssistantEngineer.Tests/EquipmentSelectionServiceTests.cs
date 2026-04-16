using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Services.Calculations;
using AssistantEngineer.Domain.Services.Equipment;
using AssistantEngineer.Infrastructure.Data;
using AssistantEngineer.Services.Calculations;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests;

public class EquipmentSelectionServiceTests
{
    [Fact]
    public async Task SelectForRoomAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.SelectForRoomAsync(
            roomId: 999,
            systemType: "Split",
            unitType: "WallMounted");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SelectForRoomAsync_WhenNoSuitableEquipment_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var room = await SeedSimpleRoomAsync(context);

        context.EquipmentCatalogItems.Add(new CoolingEquipmentCatalogItem
        {
            Id = 1,
            Manufacturer = "ACME",
            SystemType = "Split",
            UnitType = "WallMounted",
            ModelName = "WM-2.0",
            NominalCoolingCapacityKw = 2.0,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SelectForRoomAsync(
            room.Id,
            systemType: "Split",
            unitType: "WallMounted");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SelectForRoomAsync_WithSuitableEquipment_ReturnsSmallestMatchingActiveItem()
    {
        // Arrange
        await using var context = CreateContext();
        var room = await SeedRoomWithEnvelopeAndInternalLoadsAsync(context);

        context.EquipmentCatalogItems.AddRange(
            new CoolingEquipmentCatalogItem
            {
                Id = 1,
                Manufacturer = "ACME",
                SystemType = "Split",
                UnitType = "WallMounted",
                ModelName = "WM-2.4",
                NominalCoolingCapacityKw = 2.4,
                IsActive = true
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 2,
                Manufacturer = "ACME",
                SystemType = "Split",
                UnitType = "WallMounted",
                ModelName = "WM-2.6-INACTIVE",
                NominalCoolingCapacityKw = 2.6,
                IsActive = false
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 3,
                Manufacturer = "ACME",
                SystemType = "Split",
                UnitType = "WallMounted",
                ModelName = "WM-2.8",
                NominalCoolingCapacityKw = 2.8,
                IsActive = true
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 4,
                Manufacturer = "ACME",
                SystemType = "Split",
                UnitType = "Cassette",
                ModelName = "CAS-2.8",
                NominalCoolingCapacityKw = 2.8,
                IsActive = true
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 5,
                Manufacturer = "ACME",
                SystemType = "VRF",
                UnitType = "WallMounted",
                ModelName = "VRF-WM-3.5",
                NominalCoolingCapacityKw = 3.5,
                IsActive = true
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 6,
                Manufacturer = "CoolTech",
                SystemType = "Split",
                UnitType = "WallMounted",
                ModelName = "WM-3.5",
                NominalCoolingCapacityKw = 3.5,
                IsActive = true
            });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SelectForRoomAsync(
            room.Id,
            systemType: "Split",
            unitType: "WallMounted");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(room.Id, result.RoomId);
        Assert.Equal(2.33, result.TotalHeatLoadKw);
        Assert.Equal(2.56, result.DesignCapacityKw);
        Assert.Equal("Split", result.RequestedSystemType);
        Assert.Equal("WallMounted", result.RequestedUnitType);
        Assert.Equal(3, result.SelectedCatalogItemId);
        Assert.Equal("ACME", result.SelectedManufacturer);
        Assert.Equal("WM-2.8", result.SelectedModelName);
        Assert.Equal(2.8, result.SelectedNominalCoolingCapacityKw);
        Assert.Equal(0.24, result.CapacityReserveKw);
    }

    private static EquipmentSelectionService CreateService(AppDbContext context)
    {
        return new EquipmentSelectionService(
            context,
            new RoomCalculationService(),
            new CoolingEquipmentSelector());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Room> SeedSimpleRoomAsync(AppDbContext context)
    {
        var project = new Project { Id = 1, Name = "Project 1" };
        var building = new Building { Id = 1, Name = "Building 1", ProjectId = project.Id };
        var floor = new Floor { Id = 1, Name = "Floor 1", BuildingId = building.Id };
        var room = new Room
        {
            Id = 1,
            Name = "Room 1",
            FloorId = floor.Id,
            AreaM2 = 20,
            HeightM = 3,
            VolumeM3 = 60,
            IndoorTemperatureC = 24,
            OutdoorTemperatureC = 24
        };

        context.Projects.Add(project);
        context.Buildings.Add(building);
        context.Floors.Add(floor);
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        return room;
    }

    private static async Task<Room> SeedRoomWithEnvelopeAndInternalLoadsAsync(AppDbContext context)
    {
        var project = new Project { Id = 1, Name = "Project 1" };
        var building = new Building { Id = 1, Name = "Building 1", ProjectId = project.Id };
        var floor = new Floor { Id = 1, Name = "Floor 1", BuildingId = building.Id };
        var room = new Room
        {
            Id = 1,
            Name = "Room 1",
            FloorId = floor.Id,
            AreaM2 = 10,
            HeightM = 3,
            VolumeM3 = 30,
            IndoorTemperatureC = 24,
            OutdoorTemperatureC = 24,
            PeopleCount = 1,
            EquipmentLoadW = 70,
            LightingLoadW = 30
        };

        context.Projects.Add(project);
        context.Buildings.Add(building);
        context.Floors.Add(floor);
        context.Rooms.Add(room);
        context.Windows.Add(new Window { Id = 1, RoomId = room.Id, AreaM2 = 2 });
        context.Walls.AddRange(
            new Wall { Id = 1, RoomId = room.Id, AreaM2 = 10, IsExternal = true },
            new Wall { Id = 2, RoomId = room.Id, AreaM2 = 5, IsExternal = false });

        await context.SaveChangesAsync();

        return room;
    }
}
