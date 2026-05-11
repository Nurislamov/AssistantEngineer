using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneHourlySolverTests
{
    private const double Tolerance = 1e-6;

    [Fact]
    public void TwoIdenticalZonesWithoutInterZoneLink_BehaveAsIndependentSum()
    {
        var service = CreateService();
        var input = CreateTwoZoneHeatingInput(includeInterZoneConductance: false);

        var result = service.Simulate(input);

        Assert.True(result.IsValid);
        Assert.Single(result.HourlyResults);
        var hour = result.HourlyResults[0];
        var zoneAHeating = hour.HeatingLoadsByZoneW["ZONE-A"];
        var zoneBHeating = hour.HeatingLoadsByZoneW["ZONE-B"];

        AssertClose(zoneAHeating, zoneBHeating);
        AssertClose(zoneAHeating + zoneBHeating, hour.BuildingHeatingLoadW);
        AssertClose(2.0 * zoneAHeating, hour.BuildingHeatingLoadW);
    }

    [Fact]
    public void TwoZonesWithInterZoneConductance_ExchangeHeatWhenTemperaturesDiffer()
    {
        var service = CreateService();
        var withoutLink = CreateTwoZoneDriftInput(includeInterZoneConductance: false);
        var withLink = CreateTwoZoneDriftInput(includeInterZoneConductance: true);

        var baseline = service.Simulate(withoutLink);
        var coupled = service.Simulate(withLink);

        Assert.True(baseline.IsValid);
        Assert.True(coupled.IsValid);

        var baselineHour = baseline.HourlyResults[0];
        var coupledHour = coupled.HourlyResults[0];

        var baselineDifference = Math.Abs(
            baselineHour.ZoneTemperaturesCelsius["ZONE-A"] -
            baselineHour.ZoneTemperaturesCelsius["ZONE-B"]);
        var coupledDifference = Math.Abs(
            coupledHour.ZoneTemperaturesCelsius["ZONE-A"] -
            coupledHour.ZoneTemperaturesCelsius["ZONE-B"]);

        Assert.True(
            coupledDifference < baselineDifference,
            "Inter-zone conductance should reduce the temperature difference between coupled zones.");
    }

    [Fact]
    public void SameUseAdiabaticBoundary_IsNeutralForNetTransfer()
    {
        var service = CreateService();
        var withoutBoundary = CreateTwoZoneDriftInput(includeInterZoneConductance: false);
        var withAdiabaticBoundary = withoutBoundary with
        {
            BoundaryLinks =
            [
                .. withoutBoundary.BoundaryLinks,
                new ThermalZoneBoundaryLink(
                    LinkId: "ADIABATIC-SAME-USE",
                    BoundaryType: MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-ADIABATIC",
                    AreaSquareMeters: 9.0,
                    ConductanceWPerK: 100.0,
                    TargetZoneId: "ZONE-B",
                    AdjacentBoundaryCondition: new AdjacentZoneBoundaryCondition(
                        ConditionId: "SAME-USE-ADIABATIC",
                        TemperatureProfileCelsius: [20.0],
                        IsAdiabaticEquivalent: true))
            ],
            Zones =
            [
                withoutBoundary.Zones[0] with { BoundaryIds = [.. withoutBoundary.Zones[0].BoundaryIds, "A-ADIABATIC"] },
                withoutBoundary.Zones[1]
            ]
        };

        var baseline = service.Simulate(withoutBoundary);
        var adiabatic = service.Simulate(withAdiabaticBoundary);

        Assert.True(baseline.IsValid);
        Assert.True(adiabatic.IsValid);

        var baselineHour = baseline.HourlyResults[0];
        var adiabaticHour = adiabatic.HourlyResults[0];

        AssertClose(
            baselineHour.ZoneTemperaturesCelsius["ZONE-A"],
            adiabaticHour.ZoneTemperaturesCelsius["ZONE-A"]);
        AssertClose(
            baselineHour.ZoneTemperaturesCelsius["ZONE-B"],
            adiabaticHour.ZoneTemperaturesCelsius["ZONE-B"]);
        AssertClose(
            baselineHour.BuildingHeatingLoadW,
            adiabaticHour.BuildingHeatingLoadW);
        AssertClose(
            baselineHour.BuildingCoolingLoadW,
            adiabaticHour.BuildingCoolingLoadW);
    }

    [Fact]
    public void AdjacentUnconditionedBoundary_AffectsHeatingLoad()
    {
        var service = CreateService();
        var withoutAdjacent = CreateSingleZoneHeatingInput(includeAdjacentUnconditioned: false);
        var withAdjacent = CreateSingleZoneHeatingInput(includeAdjacentUnconditioned: true);

        var baseline = service.Simulate(withoutAdjacent);
        var withBoundary = service.Simulate(withAdjacent);

        Assert.True(baseline.IsValid);
        Assert.True(withBoundary.IsValid);

        var baselineHeating = baseline.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"];
        var withBoundaryHeating = withBoundary.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"];

        Assert.True(
            withBoundaryHeating > baselineHeating,
            "Adjacent unconditioned boundary should increase heating load when its boundary temperature is colder.");
    }

    [Fact]
    public void BuildingTotal_EqualsSumOfZoneTotals()
    {
        var service = CreateService();
        var result = service.Simulate(CreateTwoZoneHeatingInput(includeInterZoneConductance: true));

        Assert.True(result.IsValid);

        foreach (var hour in result.HourlyResults)
        {
            AssertClose(
                hour.HeatingLoadsByZoneW.Values.Sum(),
                hour.BuildingHeatingLoadW);
            AssertClose(
                hour.CoolingLoadsByZoneW.Values.Sum(),
                hour.BuildingCoolingLoadW);
        }

        AssertClose(
            result.AnnualSummary.AnnualHeatingEnergyByZoneKWh.Values.Sum(),
            result.AnnualSummary.AnnualHeatingEnergyTotalKWh);
        AssertClose(
            result.AnnualSummary.AnnualCoolingEnergyByZoneKWh.Values.Sum(),
            result.AnnualSummary.AnnualCoolingEnergyTotalKWh);
    }

    [Fact]
    public void TwoZoneHeatingCase_MatchesDeterministicReferenceValues()
    {
        var service = CreateService();
        var result = service.Simulate(CreateTwoZoneHeatingInput(includeInterZoneConductance: false));

        Assert.True(result.IsValid);
        Assert.Single(result.HourlyResults);

        var hour = result.HourlyResults[0];
        AssertClose(21.0, hour.ZoneTemperaturesCelsius["ZONE-A"]);
        AssertClose(21.0, hour.ZoneTemperaturesCelsius["ZONE-B"]);
        AssertClose(2013.3333333333335, hour.HeatingLoadsByZoneW["ZONE-A"]);
        AssertClose(2013.3333333333335, hour.HeatingLoadsByZoneW["ZONE-B"]);
        AssertClose(4026.666666666667, hour.BuildingHeatingLoadW);
        AssertClose(0.0, hour.BuildingCoolingLoadW);
    }

    [Fact]
    public void RepeatedRun_IsDeterministic()
    {
        var service = CreateService();
        var input = CreateTwoZoneHeatingInput(includeInterZoneConductance: true);

        var first = service.Simulate(input);
        var second = service.Simulate(input);

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        AssertEquivalentResults(first, second);
    }

    private static Iso52016MultiZoneEnergySimulationService CreateService()
    {
        var validator = new Iso52016MultiZoneInputValidator();
        var graphBuilder = new Iso52016MultiZoneGraphBuilder(validator);
        var solver = new Iso52016MultiZoneHourlySolver();
        return new Iso52016MultiZoneEnergySimulationService(validator, graphBuilder, solver);
    }

    private static MultiZoneCalculationInput CreateTwoZoneHeatingInput(bool includeInterZoneConductance)
    {
        var conductanceLinks = includeInterZoneConductance
            ? new[]
            {
                new InterZoneConductanceLink(
                    LinkId: "COND-A-B",
                    FromZoneId: "ZONE-A",
                    ToZoneId: "ZONE-B",
                    ConductanceWPerK: 10.0)
            }
            : Array.Empty<InterZoneConductanceLink>();

        return new MultiZoneCalculationInput(
            BuildingId: "BLD-HEAT",
            Zones:
            [
                new ThermalZoneNode("ZONE-A", "Zone A", 40.0, 110.0, ["A-OUT"]),
                new ThermalZoneNode("ZONE-B", "Zone B", 40.0, 110.0, ["B-OUT"])
            ],
            BoundaryLinks:
            [
                new ThermalZoneBoundaryLink("A-OUT-LINK", MultiZoneBoundaryLinkType.ExternalBoundary, "ZONE-A", "A-OUT", 10.0, 80.0),
                new ThermalZoneBoundaryLink("B-OUT-LINK", MultiZoneBoundaryLinkType.ExternalBoundary, "ZONE-B", "B-OUT", 10.0, 80.0)
            ],
            InterZoneConductanceLinks: conductanceLinks,
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions:
            [
                new MultiZoneHourlyBoundaryCondition("A-OUT", [0.0]),
                new MultiZoneHourlyBoundaryCondition("B-OUT", [0.0])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 1_200_000.0, [21.0], [26.0], [0.0], [0.0], [0.0]),
                new MultiZoneZoneHourlyProfile("ZONE-B", 20.0, 1_200_000.0, [21.0], [26.0], [0.0], [0.0], [0.0])
            ],
            ClaimFlags:
            [
                "validation anchor",
                "internal engineering anchor",
                "standard-based calculation",
                "not full validation"
            ]);
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
            BuildingId: "BLD-DRIFT",
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

    private static MultiZoneCalculationInput CreateSingleZoneHeatingInput(bool includeAdjacentUnconditioned)
    {
        var boundaryLinks = new List<ThermalZoneBoundaryLink>
        {
            new("A-OUT-LINK", MultiZoneBoundaryLinkType.ExternalBoundary, "ZONE-A", "A-OUT", 10.0, 50.0)
        };

        if (includeAdjacentUnconditioned)
        {
            boundaryLinks.Add(
                new ThermalZoneBoundaryLink(
                    LinkId: "A-UNCOND-LINK",
                    BoundaryType: MultiZoneBoundaryLinkType.AdjacentUnconditionedZone,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-UNCOND",
                    AreaSquareMeters: 8.0,
                    ConductanceWPerK: 30.0,
                    AdjacentBoundaryCondition: new AdjacentZoneBoundaryCondition(
                        ConditionId: "UNCOND-ZONE",
                        TemperatureProfileCelsius: [5.0])));
        }

        return new MultiZoneCalculationInput(
            BuildingId: "BLD-SINGLE",
            Zones:
            [
                new ThermalZoneNode("ZONE-A", "Zone A", 35.0, 100.0, ["A-OUT", "A-UNCOND"])
            ],
            BoundaryLinks: boundaryLinks,
            InterZoneConductanceLinks: [],
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions:
            [
                new MultiZoneHourlyBoundaryCondition("A-OUT", [0.0])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 1_000_000.0, [21.0], [26.0], [0.0], [0.0], [0.0])
            ],
            ClaimFlags:
            [
                "validation anchor",
                "internal engineering anchor",
                "standard-based calculation",
                "not full validation"
            ]);
    }

    private static void AssertClose(double expected, double actual) =>
        Assert.InRange(actual, expected - Tolerance, expected + Tolerance);

    private static void AssertEquivalentResults(
        MultiZoneCalculationResult expected,
        MultiZoneCalculationResult actual)
    {
        Assert.Equal(expected.HourlyResults.Count, actual.HourlyResults.Count);

        for (var hourIndex = 0; hourIndex < expected.HourlyResults.Count; hourIndex++)
        {
            var expectedHour = expected.HourlyResults[hourIndex];
            var actualHour = actual.HourlyResults[hourIndex];

            Assert.Equal(expectedHour.HourOfYear, actualHour.HourOfYear);
            AssertEqualDictionaries(expectedHour.ZoneTemperaturesCelsius, actualHour.ZoneTemperaturesCelsius);
            AssertEqualDictionaries(expectedHour.HeatingLoadsByZoneW, actualHour.HeatingLoadsByZoneW);
            AssertEqualDictionaries(expectedHour.CoolingLoadsByZoneW, actualHour.CoolingLoadsByZoneW);
            AssertClose(expectedHour.BuildingHeatingLoadW, actualHour.BuildingHeatingLoadW);
            AssertClose(expectedHour.BuildingCoolingLoadW, actualHour.BuildingCoolingLoadW);
        }

        AssertEqualDictionaries(
            expected.AnnualSummary.AnnualHeatingEnergyByZoneKWh,
            actual.AnnualSummary.AnnualHeatingEnergyByZoneKWh);
        AssertEqualDictionaries(
            expected.AnnualSummary.AnnualCoolingEnergyByZoneKWh,
            actual.AnnualSummary.AnnualCoolingEnergyByZoneKWh);
        AssertClose(expected.AnnualSummary.AnnualHeatingEnergyTotalKWh, actual.AnnualSummary.AnnualHeatingEnergyTotalKWh);
        AssertClose(expected.AnnualSummary.AnnualCoolingEnergyTotalKWh, actual.AnnualSummary.AnnualCoolingEnergyTotalKWh);

        Assert.Equal(expected.MonthlySummariesOrEmpty.Count, actual.MonthlySummariesOrEmpty.Count);
        for (var monthIndex = 0; monthIndex < expected.MonthlySummariesOrEmpty.Count; monthIndex++)
        {
            var expectedMonth = expected.MonthlySummariesOrEmpty[monthIndex];
            var actualMonth = actual.MonthlySummariesOrEmpty[monthIndex];

            Assert.Equal(expectedMonth.Month, actualMonth.Month);
            AssertEqualDictionaries(expectedMonth.HeatingEnergyByZoneKWh, actualMonth.HeatingEnergyByZoneKWh);
            AssertEqualDictionaries(expectedMonth.CoolingEnergyByZoneKWh, actualMonth.CoolingEnergyByZoneKWh);
            AssertClose(expectedMonth.BuildingHeatingEnergyKWh, actualMonth.BuildingHeatingEnergyKWh);
            AssertClose(expectedMonth.BuildingCoolingEnergyKWh, actualMonth.BuildingCoolingEnergyKWh);
        }
    }

    private static void AssertEqualDictionaries(
        IReadOnlyDictionary<string, double> expected,
        IReadOnlyDictionary<string, double> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var (key, expectedValue) in expected.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            Assert.True(actual.TryGetValue(key, out var actualValue), $"Missing key '{key}'.");
            AssertClose(expectedValue, actualValue);
        }
    }
}
