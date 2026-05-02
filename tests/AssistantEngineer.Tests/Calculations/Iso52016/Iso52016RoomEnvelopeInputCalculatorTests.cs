using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomEnvelopeInputCalculatorTests
{
    private readonly Iso52016RoomEnvelopeInputCalculator _calculator = new();

    [Fact]
    public void Calculate_ReturnsTransmissionVentilationAndCapacity()
    {
        var room = CreateRoom();

        var wallArea = Area.FromSquareMeters(10).Value;
        var wallU = ThermalTransmittance.FromValue(0.4).Value;
        var windowArea = Area.FromSquareMeters(2).Value;
        var windowU = ThermalTransmittance.FromValue(1.5).Value;
        var shgc = SolarHeatGainCoefficient.FromValue(0.6).Value;

        var wallResult = room.AddWall(
            wallArea,
            wallU,
            CardinalDirection.South,
            WallBoundaryType.External);

        Assert.True(wallResult.IsSuccess);

        var windowResult = room.AddWindow(
            windowArea,
            windowU,
            shgc,
            CardinalDirection.South);

        Assert.True(windowResult.IsSuccess);

        var result = _calculator.Calculate(
            room,
            new Iso52016RoomSimulationDefaults());

        Assert.True(result.IsSuccess);

        Assert.Equal(7.0, result.Value.TransmissionHeatTransferCoefficientWPerK, precision: 6);
        Assert.True(result.Value.VentilationHeatTransferCoefficientWPerK > 0);
        Assert.True(result.Value.ThermalCapacityJPerK > 0);
    }

    [Fact]
    public void Calculate_IgnoresAdiabaticWallsForTransmission()
    {
        var room = CreateRoom();

        var wallResult = room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.Adiabatic);

        Assert.True(wallResult.IsSuccess);

        var result = _calculator.Calculate(
            room,
            new Iso52016RoomSimulationDefaults());

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, result.Value.TransmissionHeatTransferCoefficientWPerK, precision: 6);
        Assert.True(result.Value.VentilationHeatTransferCoefficientWPerK > 0);
    }

    private static Room CreateRoom()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;
        var floor = Floor.Create("Floor", building).Value;

        return Room.Create(
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
    }
}