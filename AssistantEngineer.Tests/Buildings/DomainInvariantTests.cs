using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Domain.Enums;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class DomainInvariantTests
{
    [Fact]
    public void ProjectRejectsDuplicateBuildingNames()
    {
        var project = CreateProject();
        var first = CreateBuilding(project, "Tower");
        var second = CreateBuilding(project, "tower");

        Assert.True(project.AddBuilding(first).IsSuccess);

        var duplicate = project.AddBuilding(second);

        Assert.True(duplicate.IsFailure);
    }

    [Fact]
    public void FloorRejectsDuplicateRoomNames()
    {
        var floor = CreateFloor();

        var first = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value);
        var second = floor.AddRoom(
            "office",
            Area.FromSquareMeters(25).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
    }

    [Fact]
    public void BuildingRejectsDuplicateFloorNamesAfterTrimming()
    {
        var building = CreateBuilding();

        var first = building.AddFloor("Level 1");
        var duplicate = building.AddFloor(" Level 1 ");

        Assert.True(first.IsSuccess);
        Assert.True(duplicate.IsFailure);
    }

    [Fact]
    public void FloorRejectsDuplicateRoomNamesAfterTrimming()
    {
        var floor = CreateFloor();

        var first = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value);
        var duplicate = floor.AddRoom(
            " Office ",
            Area.FromSquareMeters(25).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value);

        Assert.True(first.IsSuccess);
        Assert.True(duplicate.IsFailure);
    }

    [Fact]
    public void RoomRejectsTotalWindowAreaAboveEightyPercentOfFloorArea()
    {
        var room = CreateRoom();
        AddExternalWall(room, CardinalDirection.South);

        var firstWindow = room.AddWindow(
            Area.FromSquareMeters(5).Value,
            ThermalTransmittance.FromValue(2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.South);

        var oversizedWindow = room.AddWindow(
            Area.FromSquareMeters(4).Value,
            ThermalTransmittance.FromValue(2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.South);

        Assert.True(firstWindow.IsSuccess);
        Assert.True(oversizedWindow.IsFailure);
    }

    [Fact]
    public void RoomRejectsWindowWithoutMatchingExternalWall()
    {
        var room = CreateRoom();
        AddExternalWall(room, CardinalDirection.North);

        var result = room.AddWindow(
            Area.FromSquareMeters(2).Value,
            ThermalTransmittance.FromValue(2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.South);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RoomStoresWindowShadingParameters()
    {
        var room = CreateRoom();
        AddExternalWall(room, CardinalDirection.South);
        var shading = WindowShadingParameters.Create(
            overhangDepthM: 0.8,
            sideFinDepthM: 0.2,
            revealDepthM: 0.1,
            windowHeightM: 1.4,
            windowWidthM: 1.8,
            minimumDirectSolarReductionFactor: 0.2,
            diffuseSolarShareUnaffected: 0.35).Value;

        var result = room.AddWindow(
            Area.FromSquareMeters(2).Value,
            ThermalTransmittance.FromValue(2).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.South,
            shading);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.8, result.Value.Shading.OverhangDepthM);
        Assert.Equal(0.2, result.Value.Shading.SideFinDepthM);
        Assert.Equal(0.35, result.Value.Shading.DiffuseSolarShareUnaffected);
    }

    internal static Project CreateProject(string name = "Project") =>
        Project.Create(name).Value;

    internal static Building CreateBuilding(Project? project = null, string name = "Building") =>
        Building.Create(name, project ?? CreateProject()).Value;

    internal static Floor CreateFloor(string name = "Floor")
    {
        var building = CreateBuilding();
        return building.AddFloor(name).Value;
    }

    internal static Room CreateRoom(string name = "Room", double areaM2 = 10)
    {
        var floor = CreateFloor();
        return floor.AddRoom(
            name,
            Area.FromSquareMeters(areaM2).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;
    }

    internal static void AddExternalWall(Room room, CardinalDirection orientation)
    {
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.2).Value,
            orientation).IsSuccess);
    }
}
