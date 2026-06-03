using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MultiZoneCouplingCharacterizationTests
{
    private const double DeterminismTolerance = 1e-9;

    [Fact]
    public void MultiZoneCoupling_CurrentBehaviorRemainsDeterministicAndFinite()
    {
        var service = CreateService();
        var withoutLink = CreateTwoZoneDriftInput(includeInterZoneConductance: false);
        var withLink = CreateTwoZoneDriftInput(includeInterZoneConductance: true);

        var baseline = service.Simulate(withoutLink);
        var first = service.Simulate(withLink);
        var second = service.Simulate(withLink);

        Assert.True(baseline.IsValid);
        Assert.True(first.IsValid);
        Assert.True(second.IsValid);

        var baselineHour = baseline.HourlyResults[0];
        var firstHour = first.HourlyResults[0];
        var secondHour = second.HourlyResults[0];

        Assert.Equal(2, firstHour.ZoneTemperaturesCelsius.Count);
        Assert.Equal(2, secondHour.ZoneTemperaturesCelsius.Count);
        Assert.Equal(firstHour.ZoneTemperaturesCelsius.Keys.OrderBy(key => key), secondHour.ZoneTemperaturesCelsius.Keys.OrderBy(key => key));

        foreach (var (_, temperature) in firstHour.ZoneTemperaturesCelsius)
        {
            Assert.False(double.IsNaN(temperature));
            Assert.False(double.IsInfinity(temperature));
        }

        foreach (var (_, load) in firstHour.HeatingLoadsByZoneW)
        {
            Assert.False(double.IsNaN(load));
            Assert.False(double.IsInfinity(load));
        }

        var baselineDifference = Math.Abs(
            baselineHour.ZoneTemperaturesCelsius["ZONE-A"] - baselineHour.ZoneTemperaturesCelsius["ZONE-B"]);
        var coupledDifference = Math.Abs(
            firstHour.ZoneTemperaturesCelsius["ZONE-A"] - firstHour.ZoneTemperaturesCelsius["ZONE-B"]);
        Assert.True(coupledDifference < baselineDifference);

        Assert.InRange(
            Math.Abs(firstHour.ZoneTemperaturesCelsius["ZONE-A"] - secondHour.ZoneTemperaturesCelsius["ZONE-A"]),
            0.0,
            DeterminismTolerance);
        Assert.InRange(
            Math.Abs(firstHour.ZoneTemperaturesCelsius["ZONE-B"] - secondHour.ZoneTemperaturesCelsius["ZONE-B"]),
            0.0,
            DeterminismTolerance);
        Assert.InRange(
            Math.Abs(firstHour.BuildingHeatingLoadW - secondHour.BuildingHeatingLoadW),
            0.0,
            DeterminismTolerance);
        Assert.InRange(
            Math.Abs(firstHour.BuildingCoolingLoadW - secondHour.BuildingCoolingLoadW),
            0.0,
            DeterminismTolerance);
    }

    private static Iso52016MultiZoneEnergySimulationService CreateService()
    {
        var validator = new Iso52016MultiZoneInputValidator();
        var graphBuilder = new Iso52016MultiZoneGraphBuilder(validator);
        var solver = new Iso52016MultiZoneHourlySolver();
        return new Iso52016MultiZoneEnergySimulationService(validator, graphBuilder, solver);
    }

    private static MultiZoneCalculationInput CreateTwoZoneDriftInput(bool includeInterZoneConductance)
    {
        var conductanceLinks = includeInterZoneConductance
            ? new[]
            {
                new InterZoneConductanceLink(
                    LinkId: "COND-DRIFT-A-B",
                    FromZoneId: "ZONE-A",
                    ToZoneId: "ZONE-B",
                    ConductanceWPerK: 60.0)
            }
            : Array.Empty<InterZoneConductanceLink>();

        return new MultiZoneCalculationInput(
            BuildingId: "BLD-DRIFT-CHARACTERIZATION",
            Zones:
            [
                new ThermalZoneNode("ZONE-A", "Zone A", 30.0, 90.0, []),
                new ThermalZoneNode("ZONE-B", "Zone B", 30.0, 90.0, [])
            ],
            BoundaryLinks: [],
            InterZoneConductanceLinks: conductanceLinks,
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions: [],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 25.0, 800_000.0, [0.0], [50.0], [0.0], [0.0], [0.0]),
                new MultiZoneZoneHourlyProfile("ZONE-B", 15.0, 800_000.0, [0.0], [50.0], [0.0], [0.0], [0.0])
            ],
            ClaimFlags:
            [
                "validation anchor",
                "internal engineering anchor",
                "standard-based calculation",
                "not full validation"
            ]);
    }
}
