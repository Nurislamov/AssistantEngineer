using AssistantEngineer.Application.Services.Rooms;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Controllers;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Services.Calculations;
using AssistantEngineer.Domain.Services.Equipment;
using AssistantEngineer.Infrastructure.Data;
using AssistantEngineer.Services.Calculations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests;

public class RoomsControllerEquipmentSelectionTests
{
    [Fact]
    public async Task SelectEquipment_WhenRoomDoesNotExist_ReturnsRoomNotFoundMessage()
    {
        // Arrange
        await using var context = CreateContext();
        var controller = CreateController(context);
        var request = new EquipmentSelectionRequest
        {
            SystemType = "Split",
            UnitType = "WallMounted"
        };

        // Act
        var actionResult = await controller.SelectEquipment(roomId: 999, request);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Equal("Room with id 999 not found.", notFound.Value);
    }

    [Fact]
    public async Task SelectEquipment_WhenNoSuitableEquipment_ReturnsNoSuitableEquipmentMessage()
    {
        // Arrange
        await using var context = CreateContext();
        var room = await SeedSimpleRoomAsync(context);
        var controller = CreateController(context);
        var request = new EquipmentSelectionRequest
        {
            SystemType = "Split",
            UnitType = "WallMounted"
        };

        // Act
        var actionResult = await controller.SelectEquipment(room.Id, request);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Equal("No suitable equipment found for the specified room and filters.", notFound.Value);
    }

    private static RoomsController CreateController(AppDbContext context)
    {
        var roomCalculationService = new RoomCalculationService();
        var coolingEquipmentSelector = new CoolingEquipmentSelector();
        var roomApplicationService = new RoomApplicationService(context, roomCalculationService);
        var equipmentSelectionService = new EquipmentSelectionService(
            context,
            roomCalculationService,
            coolingEquipmentSelector);
        return new RoomsController(roomApplicationService, equipmentSelectionService);
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
}
