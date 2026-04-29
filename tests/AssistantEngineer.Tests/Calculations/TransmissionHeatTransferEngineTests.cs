using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class TransmissionHeatTransferEngineTests
{
    private readonly TransmissionHeatTransferEngine _engine = new();

    [Fact]
    public void CalculatesExternalWallHeatLoss()
    {
        var result = Calculate(CreateElement(
            elementType: TransmissionElementType.Wall,
            areaM2: 10,
            uValueWPerM2K: 0.5,
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            boundaryType: TransmissionBoundaryType.Outdoor));

        Assert.False(result.HasErrors);
        Assert.Equal(25, result.Elements.Single().DeltaTC);
        Assert.Equal(125, result.Elements.Single().HeatFlowW);
        Assert.Equal(125, result.TotalHeatLossW);
        Assert.Equal(0, result.TotalHeatGainW);
    }

    [Fact]
    public void CalculatesWindowHeatLoss()
    {
        var result = Calculate(CreateElement(
            elementType: TransmissionElementType.Window,
            areaM2: 2,
            uValueWPerM2K: 2.0,
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            boundaryType: TransmissionBoundaryType.Outdoor));

        Assert.False(result.HasErrors);
        Assert.Equal(25, result.Elements.Single().DeltaTC);
        Assert.Equal(100, result.Elements.Single().HeatFlowW);
        Assert.Equal(100, result.TotalHeatLossW);
    }

    [Fact]
    public void AdiabaticBoundaryReturnsZeroHeatFlow()
    {
        var result = Calculate(CreateElement(
            elementType: TransmissionElementType.Wall,
            areaM2: 12,
            uValueWPerM2K: 1.0,
            indoorTemperatureC: 20,
            boundaryType: TransmissionBoundaryType.InternalAdiabatic));

        var element = result.Elements.Single();
        Assert.Equal(0, element.HeatFlowW);
        Assert.False(element.IsIncludedInLoad);
        Assert.Contains(element.Diagnostics, diagnostic => diagnostic.Code == "Transmission.InternalAdiabatic");
    }

    [Fact]
    public void AdjacentConditionedZoneWithSameTemperatureReturnsZero()
    {
        var result = Calculate(CreateElement(
            elementType: TransmissionElementType.Wall,
            areaM2: 8,
            uValueWPerM2K: 0.7,
            indoorTemperatureC: 22,
            adjacentTemperatureC: 22,
            boundaryType: TransmissionBoundaryType.AdjacentConditionedZone));

        var element = result.Elements.Single();
        Assert.True(element.IsIncludedInLoad);
        Assert.Equal(0, element.DeltaTC);
        Assert.Equal(0, element.HeatFlowW);
        Assert.Equal(0, result.TotalHeatFlowW);
    }

    [Fact]
    public void CoolingCaseRepresentsHeatGainDirection()
    {
        var result = Calculate(CreateElement(
            elementType: TransmissionElementType.Wall,
            areaM2: 10,
            uValueWPerM2K: 0.5,
            indoorTemperatureC: 24,
            outdoorTemperatureC: 34,
            boundaryType: TransmissionBoundaryType.Outdoor));

        var element = result.Elements.Single();
        Assert.Equal(-10, element.DeltaTC);
        Assert.Equal(-50, element.HeatFlowW);
        Assert.Equal(-50, result.TotalHeatFlowW);
        Assert.Equal(0, result.TotalHeatLossW);
        Assert.Equal(50, result.TotalHeatGainW);
    }

    [Fact]
    public void InvalidAreaProducesDiagnosticsAndExcludesElement()
    {
        var result = Calculate(CreateElement(
            elementType: TransmissionElementType.Wall,
            areaM2: 0,
            uValueWPerM2K: 0.5,
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            boundaryType: TransmissionBoundaryType.Outdoor));

        Assert.True(result.HasErrors);
        Assert.Equal(0, result.TotalHeatFlowW);
        Assert.False(result.Elements.Single().IsIncludedInLoad);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Transmission.InvalidArea");
    }

    [Fact]
    public void InvalidUValueProducesDiagnosticsAndExcludesElement()
    {
        var result = Calculate(CreateElement(
            elementType: TransmissionElementType.Wall,
            areaM2: 10,
            uValueWPerM2K: 0,
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            boundaryType: TransmissionBoundaryType.Outdoor));

        Assert.True(result.HasErrors);
        Assert.Equal(0, result.TotalHeatFlowW);
        Assert.False(result.Elements.Single().IsIncludedInLoad);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Transmission.InvalidUValue");
    }

    [Fact]
    public async Task RoomHeatingCalculationUsesTransmissionEngineResult()
    {
        var room = CreateRoomWithWall(
            wallAreaM2: 10,
            wallUValue: 0.5,
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5);
        var calculator = new En12831HeatingLoadCalculator(
            Options.Create(new En12831HeatingLoadOptions()),
            _engine);

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(125, result.TransmissionHeatLossW);
    }

    [Fact]
    public async Task BuildingAggregationIncludesRoomTransmissionWithoutDoubleCounting()
    {
        var project = DomainInvariantTests.CreateProject();
        var climateZone = ClimateZone.Create(
            "Transmission aggregation climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var building = Building.Create("Transmission building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        var firstRoom = floor.AddRoom(
            "Office 101",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var secondRoom = floor.AddRoom(
            "Office 102",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            Temperature.FromCelsius(-5).Value).Value;

        Assert.True(firstRoom.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.5).Value,
            CardinalDirection.South).IsSuccess);
        Assert.True(secondRoom.AddWall(
            Area.FromSquareMeters(8).Value,
            ThermalTransmittance.FromValue(0.5).Value,
            CardinalDirection.South).IsSuccess);

        var calculator = new En12831HeatingLoadCalculator(
            Options.Create(new En12831HeatingLoadOptions()),
            _engine);

        var result = await calculator.CalculateAsync(building);

        Assert.Equal(225, result.TransmissionHeatLossW);
        Assert.Equal(2, result.Rooms.Count);
        Assert.Equal(
            result.Rooms.Sum(room => room.TransmissionHeatLossW),
            result.TransmissionHeatLossW);
    }

    private TransmissionHeatTransferResult Calculate(
        TransmissionElementInput element)
    {
        var result = _engine.Calculate(new TransmissionHeatTransferRequest([element]));
        Assert.True(result.IsSuccess, result.Error);
        return result.Value;
    }

    private static TransmissionElementInput CreateElement(
        TransmissionElementType elementType,
        double areaM2,
        double uValueWPerM2K,
        double indoorTemperatureC,
        TransmissionBoundaryType boundaryType,
        double? outdoorTemperatureC = null,
        double? adjacentTemperatureC = null) =>
        new(
            ElementId: 1,
            ElementType: elementType,
            RoomId: 101,
            AreaM2: areaM2,
            UValueWPerM2K: uValueWPerM2K,
            IndoorTemperatureC: indoorTemperatureC,
            BoundaryType: boundaryType,
            OutdoorTemperatureC: outdoorTemperatureC,
            AdjacentTemperatureC: adjacentTemperatureC);

    private static Room CreateRoomWithWall(
        double wallAreaM2,
        double wallUValue,
        double indoorTemperatureC,
        double outdoorTemperatureC)
    {
        var project = DomainInvariantTests.CreateProject();
        var building = Building.Create("Building", project).Value;
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(indoorTemperatureC).Value,
            Temperature.FromCelsius(outdoorTemperatureC).Value).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(wallAreaM2).Value,
            ThermalTransmittance.FromValue(wallUValue).Value,
            CardinalDirection.South).IsSuccess);

        return room;
    }
}
