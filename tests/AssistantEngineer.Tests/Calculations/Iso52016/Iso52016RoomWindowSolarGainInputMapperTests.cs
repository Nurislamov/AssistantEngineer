using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomWindowSolarGainInputMapperTests
{
    private readonly Iso52016RoomWindowSolarGainInputMapper _mapper = new();

    [Fact]
    public void Map_ReturnsWindowInputsFromRoomWindows()
    {
        var room = CreateRoomWithExternalWall();

        var windowResult = room.AddWindow(
            area: Area.FromSquareMeters(2).Value,
            uValue: ThermalTransmittance.FromValue(1.5).Value,
            shgc: SolarHeatGainCoefficient.FromValue(0.6).Value,
            orientation: CardinalDirection.South);

        Assert.True(windowResult.IsSuccess);

        var result = _mapper.Map(
            room,
            new Iso52016RoomSimulationDefaults());

        Assert.True(result.IsSuccess);

        var input = Assert.Single(result.Value);

        Assert.Equal("window-1", input.WindowCode);
        Assert.Equal(CardinalDirection.South, input.Orientation);
        Assert.Equal(2.0, input.WindowAreaM2);
        Assert.Equal(0.6, input.SolarHeatGainCoefficient);
        Assert.Equal(0.25, input.FrameFraction);
        Assert.Equal(1.0, input.ShadingFactor);
    }

    [Fact]
    public void Map_WithShadingGeometry_AppliesSimplifiedShadingFactor()
    {
        var room = CreateRoomWithExternalWall();

        var shading = WindowShadingParameters.Create(
            overhangDepthM: 0.5,
            minimumDirectSolarReductionFactor: 0.2,
            diffuseSolarShareUnaffected: 0.4).Value;

        var windowResult = room.AddWindow(
            area: Area.FromSquareMeters(2).Value,
            uValue: ThermalTransmittance.FromValue(1.5).Value,
            shgc: SolarHeatGainCoefficient.FromValue(0.6).Value,
            orientation: CardinalDirection.South,
            shading: shading);

        Assert.True(windowResult.IsSuccess);

        var result = _mapper.Map(
            room,
            new Iso52016RoomSimulationDefaults());

        Assert.True(result.IsSuccess);

        var input = Assert.Single(result.Value);

        Assert.Equal(0.4, input.ShadingFactor);
    }

    private static Room CreateRoomWithExternalWall()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;
        var floor = Floor.Create("Floor", building).Value;

        var room = Room.Create(
            name: "Room 1",
            area: Area.FromSquareMeters(20).Value,
            heightM: 3,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            floor: floor,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office).Value;

        var wallResult = room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External);

        Assert.True(wallResult.IsSuccess);

        return room;
    }
}