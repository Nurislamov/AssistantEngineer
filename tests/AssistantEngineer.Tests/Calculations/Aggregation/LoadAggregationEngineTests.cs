using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;

namespace AssistantEngineer.Tests.Calculations.Aggregation;

public class LoadAggregationEngineTests
{
    private readonly LoadAggregationEngine _engine = new();

    [Fact]
    public void Aggregate_FloorTwoRoomsReturnsExpectedTotals()
    {
        var result = _engine.Aggregate(new LoadAggregationInput(
            TargetId: 10,
            TargetType: LoadAggregationTargetType.Floor,
            Rooms:
            [
                Room(1, floorId: 10, heatingW: 1000, coolingW: 800, areaM2: 20),
                Room(2, floorId: 10, heatingW: 500, coolingW: 700, areaM2: 10)
            ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.RoomCount);
        Assert.Equal(30, result.Value.TotalAreaM2, precision: 6);
        Assert.Equal(1500, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(1500, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(50, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal(50, result.Value.CoolingLoadWPerM2, precision: 6);
    }

    [Fact]
    public void Aggregate_BuildingTwoFloorsReturnsExpectedTotal()
    {
        var result = _engine.Aggregate(new LoadAggregationInput(
            TargetId: 100,
            TargetType: LoadAggregationTargetType.Building,
            Rooms:
            [
                Room(1, buildingId: 100, floorId: 10, heatingW: 1500, coolingW: 800, areaM2: 30),
                Room(2, buildingId: 100, floorId: 11, heatingW: 2500, coolingW: 900, areaM2: 40)
            ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(4000, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(1700, result.Value.CoolingLoadW, precision: 6);
    }

    [Fact]
    public void Aggregate_ThermalZoneDoesNotDoubleCountDuplicatedRooms()
    {
        var result = _engine.Aggregate(new LoadAggregationInput(
            TargetId: 5,
            TargetType: LoadAggregationTargetType.ThermalZone,
            Rooms:
            [
                Room(1, zoneId: 5, heatingW: 1000, coolingW: 800, areaM2: 20),
                Room(1, zoneId: 5, heatingW: 1000, coolingW: 800, areaM2: 20),
                Room(2, zoneId: 5, heatingW: 500, coolingW: 700, areaM2: 10),
                Room(3, zoneId: 6, heatingW: 900, coolingW: 900, areaM2: 10)
            ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.RoomCount);
        Assert.Equal(1500, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(1500, result.Value.CoolingLoadW, precision: 6);
    }

    [Fact]
    public void Aggregate_SumsComponentBreakdown()
    {
        var result = _engine.Aggregate(new LoadAggregationInput(
            TargetId: 100,
            TargetType: LoadAggregationTargetType.Building,
            Rooms:
            [
                Room(
                    1,
                    buildingId: 100,
                    heatingW: 1000,
                    coolingW: 1500,
                    areaM2: 20,
                    heatingBreakdown: new RoomHeatingLoadBreakdown(TransmissionW: 700, WindowTransmissionW: 100, GroundW: 0, VentilationW: 150, InfiltrationW: 50, UsefulSolarGainOffsetW: 0, UsefulInternalGainOffsetW: 0),
                    coolingBreakdown: new RoomCoolingLoadBreakdown(TransmissionW: 200, WindowTransmissionW: 100, SolarW: 600, VentilationW: 300, InfiltrationW: 100, InternalGainsW: 200, GroundW: 0))
            ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(1100, result.Value.ComponentBreakdown.TransmissionW, precision: 6);
        Assert.Equal(600, result.Value.ComponentBreakdown.SolarW, precision: 6);
        Assert.Equal(450, result.Value.ComponentBreakdown.VentilationW, precision: 6);
        Assert.Equal(150, result.Value.ComponentBreakdown.InfiltrationW, precision: 6);
        Assert.Equal(200, result.Value.ComponentBreakdown.InternalW, precision: 6);
    }

    [Fact]
    public void Aggregate_HourlyModeUsesCoincidentPeak()
    {
        var result = _engine.Aggregate(new LoadAggregationInput(
            TargetId: 100,
            TargetType: LoadAggregationTargetType.Building,
            Rooms:
            [
                Room(1, buildingId: 100, heatingW: 1000, coolingW: 1000, areaM2: 20, hourlyHeating: [1000, 100], hourlyCooling: [100, 700]),
                Room(2, buildingId: 100, heatingW: 1000, coolingW: 1000, areaM2: 20, hourlyHeating: [100, 800], hourlyCooling: [600, 100])
            ],
            Mode: LoadAggregationMode.Hourly));

        Assert.True(result.IsSuccess);
        Assert.Equal(1100, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(800, result.Value.CoolingLoadW, precision: 6);
        Assert.Contains("Hourly", result.Value.AggregationMethod);
    }

    [Fact]
    public void Aggregate_NoRoomsAddsDiagnostic()
    {
        var result = _engine.Aggregate(new LoadAggregationInput(
            TargetId: 100,
            TargetType: LoadAggregationTargetType.Building,
            Rooms: []));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.RoomCount);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "Aggregation.NoRooms");
    }

    private static AggregationRoomLoadInput Room(
        int roomId,
        int? zoneId = null,
        int? floorId = 1,
        int buildingId = 100,
        double heatingW = 0,
        double coolingW = 0,
        double areaM2 = 1,
        RoomHeatingLoadBreakdown? heatingBreakdown = null,
        RoomCoolingLoadBreakdown? coolingBreakdown = null,
        IReadOnlyList<double>? hourlyHeating = null,
        IReadOnlyList<double>? hourlyCooling = null) =>
        new(
            roomId,
            $"Room {roomId}",
            zoneId,
            floorId,
            buildingId,
            areaM2,
            heatingW,
            coolingW,
            heatingBreakdown,
            coolingBreakdown,
            hourlyHeating,
            hourlyCooling);
}
