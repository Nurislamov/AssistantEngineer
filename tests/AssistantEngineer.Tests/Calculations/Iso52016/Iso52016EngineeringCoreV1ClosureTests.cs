using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016EngineeringCoreV1ClosureTests
{
    [Fact]
    public void SimplifiedHourlyHeatBalance_EndToEnd8760ScenarioProducesConsistentHeatingCoolingAndMonthlySummaries()
    {
        var result = Iso52016MatrixTestSolver.Solve(
            CreateSeasonal8760InputProfile(),
            new Iso52016RoomHeatBalanceOptions(
                InitialIndoorTemperatureC: 22,
                TimeStepSeconds: 3600));

        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;

        Assert.Equal("engineering-core-v1-room", profile.RoomCode);
        Assert.Equal(8760, profile.HourCount);
        Assert.Equal(12, profile.MonthlySummaries.Count);
        Assert.Equal(20, profile.HeatingSetpointC);
        Assert.Equal(26, profile.CoolingSetpointC);

        Assert.True(profile.AnnualHeatingEnergyKWh > 0);
        Assert.True(profile.AnnualCoolingEnergyKWh > 0);
        Assert.True(profile.AnnualSolarGainsKWh > 0);
        Assert.True(profile.AnnualInternalGainsKWh > 0);
        Assert.True(profile.AnnualTotalGainsKWh > 0);

        Assert.Equal(
            profile.AnnualHeatingEnergyKWh,
            profile.MonthlySummaries.Sum(month => month.HeatingEnergyKWh),
            precision: 6);

        Assert.Equal(
            profile.AnnualCoolingEnergyKWh,
            profile.MonthlySummaries.Sum(month => month.CoolingEnergyKWh),
            precision: 6);

        Assert.Equal(
            profile.AnnualSolarGainsKWh,
            profile.MonthlySummaries.Sum(month => month.SolarGainsKWh),
            precision: 6);

        Assert.Equal(
            profile.AnnualInternalGainsKWh,
            profile.MonthlySummaries.Sum(month => month.InternalGainsKWh),
            precision: 6);

        Assert.Equal(
            profile.AnnualTotalGainsKWh,
            profile.MonthlySummaries.Sum(month => month.TotalGainsKWh),
            precision: 6);

        Assert.Equal(
            Enumerable.Range(0, 8760),
            profile.Hours.Select(hour => hour.HourOfYear));

        Assert.DoesNotContain(profile.Hours, hour =>
            hour.HeatingLoadW > 0 &&
            hour.CoolingLoadW > 0);

        Assert.All(profile.Hours, hour =>
        {
            Assert.True(hour.HeatingLoadW >= 0);
            Assert.True(hour.CoolingLoadW >= 0);
            Assert.True(hour.HeatingEnergyKWh >= 0);
            Assert.True(hour.CoolingEnergyKWh >= 0);

            if (hour.HeatingLoadW > 0)
            {
                Assert.Equal(
                    hour.HeatingSetpointC,
                    hour.IndoorTemperatureAfterHvacC,
                    precision: 6);
            }

            if (hour.CoolingLoadW > 0)
            {
                Assert.Equal(
                    hour.CoolingSetpointC,
                    hour.IndoorTemperatureAfterHvacC,
                    precision: 6);
            }
        });

        var january = Assert.Single(
            profile.MonthlySummaries,
            month => month.Month == 1);

        var july = Assert.Single(
            profile.MonthlySummaries,
            month => month.Month == 7);

        Assert.True(january.HeatingEnergyKWh > 0);
        Assert.Equal(0, january.CoolingEnergyKWh, precision: 6);
        Assert.True(january.PeakHeatingLoadW > 0);

        Assert.True(july.CoolingEnergyKWh > 0);
        Assert.Equal(0, july.HeatingEnergyKWh, precision: 6);
        Assert.True(july.PeakCoolingLoadW > 0);

        Assert.True(profile.PeakHeatingLoadW > 0);
        Assert.True(profile.PeakCoolingLoadW > 0);
    }

    [Fact]
    public void SingleThermalZoneAggregation_HourlyModeAggregatesAssignedRoomsOnlyWithoutDoubleCounting()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 10,
            TargetType: LoadAggregationTargetType.ThermalZone,
            TargetName: "Single engineering zone",
            Mode: LoadAggregationMode.Hourly,
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Zone room 1",
                    ThermalZoneId: 10,
                    FloorId: 1,
                    BuildingId: 100,
                    AreaM2: 20,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 800,
                    HourlyHeatingLoadW: [100, 200, 300, 400],
                    HourlyCoolingLoadW: [50, 100, 150, 200]),

                new AggregationRoomLoadInput(
                    RoomId: 2,
                    RoomName: "Zone room 2",
                    ThermalZoneId: 10,
                    FloorId: 1,
                    BuildingId: 100,
                    AreaM2: 30,
                    HeatingLoadW: 1500,
                    CoolingLoadW: 900,
                    HourlyHeatingLoadW: [25, 50, 75, 100],
                    HourlyCoolingLoadW: [10, 20, 30, 40]),

                new AggregationRoomLoadInput(
                    RoomId: 3,
                    RoomName: "Other zone room",
                    ThermalZoneId: 20,
                    FloorId: 1,
                    BuildingId: 100,
                    AreaM2: 999,
                    HeatingLoadW: 99_999,
                    CoolingLoadW: 99_999,
                    HourlyHeatingLoadW: [99_999, 99_999, 99_999, 99_999],
                    HourlyCoolingLoadW: [99_999, 99_999, 99_999, 99_999])
            ]));

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(10, result.Value.TargetId);
        Assert.Equal(LoadAggregationTargetType.ThermalZone, result.Value.TargetType);
        Assert.Equal("Single engineering zone", result.Value.TargetName);

        Assert.Equal(2, result.Value.RoomCount);
        Assert.Equal(50, result.Value.TotalAreaM2, precision: 6);

        Assert.Equal(500, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(240, result.Value.CoolingLoadW, precision: 6);

        Assert.Equal(10, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal(4.8, result.Value.CoolingLoadWPerM2, precision: 6);

        Assert.Equal(
            [1, 2],
            result.Value.RoomBreakdown.Select(room => room.RoomId).ToArray());

        Assert.DoesNotContain(
            result.Value.RoomBreakdown,
            room => room.RoomId == 3);

        Assert.Contains(
            "Hourly Coincident Load Aggregation",
            result.Value.AggregationMethod,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "Aggregation.HourlyUnavailable");
    }

    [Fact]
    public void SingleThermalZoneAggregation_DesignPointModeAggregatesAssignedRoomComponentsOnly()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 10,
            TargetType: LoadAggregationTargetType.ThermalZone,
            TargetName: "Single engineering zone",
            Mode: LoadAggregationMode.DesignPoint,
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Zone room 1",
                    ThermalZoneId: 10,
                    FloorId: 1,
                    BuildingId: 100,
                    AreaM2: 20,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 800),

                new AggregationRoomLoadInput(
                    RoomId: 2,
                    RoomName: "Zone room 2",
                    ThermalZoneId: 10,
                    FloorId: 1,
                    BuildingId: 100,
                    AreaM2: 30,
                    HeatingLoadW: 1500,
                    CoolingLoadW: 900),

                new AggregationRoomLoadInput(
                    RoomId: 3,
                    RoomName: "Other zone room",
                    ThermalZoneId: 20,
                    FloorId: 1,
                    BuildingId: 100,
                    AreaM2: 999,
                    HeatingLoadW: 99_999,
                    CoolingLoadW: 99_999)
            ]));

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(2, result.Value.RoomCount);
        Assert.Equal(50, result.Value.TotalAreaM2, precision: 6);
        Assert.Equal(2500, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(1700, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(50, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal(34, result.Value.CoolingLoadWPerM2, precision: 6);

        Assert.Equal(
            [1, 2],
            result.Value.RoomBreakdown.Select(room => room.RoomId).ToArray());

        Assert.DoesNotContain(
            result.Value.RoomBreakdown,
            room => room.RoomId == 3);

        Assert.Contains(
            "Design Point Load Aggregation",
            result.Value.AggregationMethod,
            StringComparison.OrdinalIgnoreCase);
    }

    private static Iso52016RoomHourlyInputProfile CreateSeasonal8760InputProfile()
    {
        var hours = new List<Iso52016RoomHourlyInputRecord>(8760);
        var hourOfYear = 0;

        foreach (var (month, hoursInMonth) in MonthHours())
        {
            for (var hourInMonth = 0; hourInMonth < hoursInMonth; hourInMonth++)
            {
                var hourOfDay = hourInMonth % 24;
                var outdoorTemperatureC = OutdoorTemperatureForMonth(month);
                var solarGainsW = SolarGainsForMonthAndHour(month, hourOfDay);
                var internalGainsW = hourOfDay is >= 8 and <= 17 ? 450 : 120;
                var transmissionH = 110.0;
                var ventilationH = 25.0;
                var totalH = transmissionH + ventilationH;

                hours.Add(new Iso52016RoomHourlyInputRecord(
                    HourOfYear: hourOfYear++,
                    Month: month,
                    Day: Math.Max(1, hourInMonth / 24 + 1),
                    Hour: hourOfDay,
                    OutdoorTemperatureC: outdoorTemperatureC,
                    GroundBoundaryTemperatureC: outdoorTemperatureC,
                    HeatingSetpointC: 20,
                    CoolingSetpointC: 26,
                    TransmissionHeatTransferCoefficientWPerK: transmissionH,
                    VentilationHeatTransferCoefficientWPerK: ventilationH,
                    TotalHeatTransferCoefficientWPerK: totalH,
                    ThermalCapacityJPerK: 4_500_000,
                    SolarGainsW: solarGainsW,
                    InternalGainsW: internalGainsW,
                    TotalGainsW: solarGainsW + internalGainsW));
            }
        }

        Assert.Equal(8760, hours.Count);
        Assert.Equal(Enumerable.Range(0, 8760), hours.Select(hour => hour.HourOfYear));

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: "engineering-core-v1-room",
            TransmissionHeatTransferCoefficientWPerK: 110,
            VentilationHeatTransferCoefficientWPerK: 25,
            ThermalCapacityJPerK: 4_500_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: hours);
    }

    private static double OutdoorTemperatureForMonth(int month) =>
        month switch
        {
            12 or 1 or 2 => -5,
            3 or 11 => 5,
            4 or 10 => 14,
            5 or 9 => 22,
            6 or 7 or 8 => 34,
            _ => 20
        };

    private static double SolarGainsForMonthAndHour(int month, int hourOfDay)
    {
        var isDaytime = hourOfDay is >= 8 and <= 17;

        if (!isDaytime)
            return 0;

        return month switch
        {
            12 or 1 or 2 => 100,
            3 or 4 or 10 or 11 => 300,
            5 or 6 or 7 or 8 or 9 => 700,
            _ => 250
        };
    }

    private static IEnumerable<(int Month, int Hours)> MonthHours()
    {
        var daysPerMonth = new[]
        {
            31,
            28,
            31,
            30,
            31,
            30,
            31,
            31,
            30,
            31,
            30,
            31
        };

        for (var month = 1; month <= 12; month++)
            yield return (month, daysPerMonth[month - 1] * 24);
    }
}