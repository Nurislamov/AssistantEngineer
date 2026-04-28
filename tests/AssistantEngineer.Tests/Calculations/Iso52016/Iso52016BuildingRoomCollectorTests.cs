using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016BuildingRoomCollectorTests
{
    private readonly Iso52016BuildingRoomCollector _collector = new();

    [Fact]
    public void CollectRooms_ReturnsRoomsFromAllFloors()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;

        var firstFloor = building.AddFloor("Floor 1").Value;
        var secondFloor = building.AddFloor("Floor 2").Value;

        var firstRoom = firstFloor.AddRoom(
            "Room 1",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value).Value;

        var secondRoom = secondFloor.AddRoom(
            "Room 2",
            Area.FromSquareMeters(15).Value,
            3,
            Temperature.FromCelsius(20).Value).Value;

        var result = _collector.CollectRooms(building);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value.Count);
        Assert.Contains(firstRoom, result.Value);
        Assert.Contains(secondRoom, result.Value);
    }

    [Fact]
    public void CollectRooms_RejectsBuildingWithoutRooms()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;

        Assert.True(building.AddFloor("Floor 1").IsSuccess);

        var result = _collector.CollectRooms(building);

        Assert.True(result.IsFailure);
        Assert.Equal("Building must contain at least one room.", result.Error);
    }

    [Fact]
    public void CollectRooms_RejectsDuplicateRoomNamesAcrossFloors()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;

        var firstFloor = building.AddFloor("Floor 1").Value;
        var secondFloor = building.AddFloor("Floor 2").Value;

        Assert.True(firstFloor.AddRoom(
            "Room 1",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value).IsSuccess);

        Assert.True(secondFloor.AddRoom(
            "room 1",
            Area.FromSquareMeters(15).Value,
            3,
            Temperature.FromCelsius(20).Value).IsSuccess);

        var result = _collector.CollectRooms(building);

        Assert.True(result.IsFailure);
        Assert.Contains("Room names must be unique", result.Error);
    }
}