using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;

namespace AssistantEngineer.Tests.Calculations;

public class LoadAggregationHourlyProfileDiagnosticsTests
{
    [Fact]
    public void HourlyAggregationWithMatchedProfilesUsesCoincidentPeakAndReportsRecordCount()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 900,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Matched hourly building",
            Mode: LoadAggregationMode.Hourly,
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Room 1",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: 20,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 800,
                    HourlyHeatingLoadW: [100, 600, 200],
                    HourlyCoolingLoadW: [50, 500, 300]),

                new AggregationRoomLoadInput(
                    RoomId: 2,
                    RoomName: "Room 2",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: 30,
                    HeatingLoadW: 900,
                    CoolingLoadW: 700,
                    HourlyHeatingLoadW: [200, 100, 500],
                    HourlyCoolingLoadW: [100, 200, 600])
            ],
            DiagnosticsContext: "aggregation-hourly-matched"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(700, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(900, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(3, result.Value.HourlyRecordCountUsed);
        Assert.Contains("Hourly Coincident", result.Value.AggregationMethod, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "Aggregation.HourlyProfileLengthMismatch");
    }

    [Fact]
    public void HourlyAggregationWithMismatchedProfilesUsesShortestCommonLengthWithWarning()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 901,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Mismatched hourly building",
            Mode: LoadAggregationMode.Hourly,
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Room 1",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 901,
                    AreaM2: 20,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 800,
                    HourlyHeatingLoadW: [100, 600, 200],
                    HourlyCoolingLoadW: [50, 500, 300]),

                new AggregationRoomLoadInput(
                    RoomId: 2,
                    RoomName: "Room 2",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 901,
                    AreaM2: 30,
                    HeatingLoadW: 900,
                    CoolingLoadW: 700,
                    HourlyHeatingLoadW: [200, 100],
                    HourlyCoolingLoadW: [100, 200])
            ],
            DiagnosticsContext: "aggregation-hourly-mismatch"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(700, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(700, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(2, result.Value.HourlyRecordCountUsed);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "Aggregation.HourlyProfileLengthMismatch" &&
            diagnostic.Context == "aggregation-hourly-mismatch");
    }

    [Fact]
    public void HourlyAggregationFallbackKeepsNullHourlyRecordCountUsed()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 902,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Fallback hourly building",
            Mode: LoadAggregationMode.Hourly,
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Room 1",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 902,
                    AreaM2: 20,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 800)
            ],
            DiagnosticsContext: "aggregation-hourly-fallback"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(1000, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(800, result.Value.CoolingLoadW, precision: 6);
        Assert.Null(result.Value.HourlyRecordCountUsed);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "Aggregation.HourlyUnavailable");
    }
}
