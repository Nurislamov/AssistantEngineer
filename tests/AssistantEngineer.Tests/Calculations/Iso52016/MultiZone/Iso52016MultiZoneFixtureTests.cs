using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneFixtureTests
{
    private const double Tolerance = 1e-6;

    [Fact]
    public void FixtureClaimBoundary_StaysWithinInternalAnalyticalAnchorScope()
    {
        var fixtures = LoadAllFixtures();

        Assert.NotEmpty(fixtures);

        foreach (var fixture in fixtures)
        {
            Assert.Contains(
                fixture.ClaimBoundary,
                line => line.Contains("internal", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                fixture.ClaimBoundary,
                line => line.Contains("full ISO52016 compliance claim", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                fixture.ClaimBoundary,
                line => line.Contains("external validation claim", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void TwoZoneIndependentFixture_BehavesAsIndependentSum()
    {
        var service = CreateService();
        var fixture = LoadFixture("two-zone-independent");

        var result = service.Simulate(fixture.Input);

        Assert.True(result.IsValid);
        Assert.Single(result.HourlyResults);

        var hour = result.HourlyResults[0];
        var zoneAHeating = hour.HeatingLoadsByZoneW["ZONE-A"];
        var zoneBHeating = hour.HeatingLoadsByZoneW["ZONE-B"];

        AssertClose(zoneAHeating, zoneBHeating);
        AssertClose(zoneAHeating + zoneBHeating, hour.BuildingHeatingLoadW);
    }

    [Fact]
    public void TwoZoneInterZoneConductanceFixture_ExchangesHeatWhenTemperaturesDiffer()
    {
        var service = CreateService();
        var coupled = LoadFixture("two-zone-interzone-conductance").Input;
        var decoupled = coupled with { InterZoneConductanceLinks = [] };

        var coupledResult = service.Simulate(coupled);
        var decoupledResult = service.Simulate(decoupled);

        Assert.True(coupledResult.IsValid);
        Assert.True(decoupledResult.IsValid);

        var coupledHour = coupledResult.HourlyResults[0];
        var decoupledHour = decoupledResult.HourlyResults[0];

        var coupledDifference = Math.Abs(
            coupledHour.ZoneTemperaturesCelsius["ZONE-A"] - coupledHour.ZoneTemperaturesCelsius["ZONE-B"]);
        var decoupledDifference = Math.Abs(
            decoupledHour.ZoneTemperaturesCelsius["ZONE-A"] - decoupledHour.ZoneTemperaturesCelsius["ZONE-B"]);

        Assert.True(
            coupledDifference < decoupledDifference,
            "Inter-zone conductance should reduce the zone temperature difference in this analytical anchor fixture.");
    }

    [Fact]
    public void AdjacentUnconditionedFixture_IncreasesHeatingNeed()
    {
        var service = CreateService();
        var withAdjacent = LoadFixture("adjacent-unconditioned-zone").Input;
        var withoutAdjacent = withAdjacent with
        {
            BoundaryLinks = withAdjacent.BoundaryLinks
                .Where(link => link.BoundaryType != MultiZoneBoundaryLinkType.AdjacentUnconditionedZone)
                .ToArray(),
            Zones =
            [
                withAdjacent.Zones[0] with { BoundaryIds = ["A-OUT"] }
            ]
        };

        var withAdjacentResult = service.Simulate(withAdjacent);
        var withoutAdjacentResult = service.Simulate(withoutAdjacent);

        Assert.True(withAdjacentResult.IsValid);
        Assert.True(withoutAdjacentResult.IsValid);

        var withAdjacentHeating = withAdjacentResult.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"];
        var withoutAdjacentHeating = withoutAdjacentResult.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"];

        Assert.True(
            withAdjacentHeating > withoutAdjacentHeating,
            "Adjacent unconditioned boundary should increase heating need when adjacent boundary temperature is colder.");
    }

    [Fact]
    public void SameUseAdiabaticFixture_IsNeutralAgainstIndependentFixture()
    {
        var service = CreateService();
        var adiabatic = LoadFixture("same-use-adiabatic-boundary").Input;
        var independent = LoadFixture("two-zone-independent").Input;

        var adiabaticResult = service.Simulate(adiabatic);
        var independentResult = service.Simulate(independent);

        Assert.True(adiabaticResult.IsValid);
        Assert.True(independentResult.IsValid);

        var adiabaticHour = adiabaticResult.HourlyResults[0];
        var independentHour = independentResult.HourlyResults[0];

        AssertClose(independentHour.ZoneTemperaturesCelsius["ZONE-A"], adiabaticHour.ZoneTemperaturesCelsius["ZONE-A"]);
        AssertClose(independentHour.ZoneTemperaturesCelsius["ZONE-B"], adiabaticHour.ZoneTemperaturesCelsius["ZONE-B"]);
        AssertClose(independentHour.BuildingHeatingLoadW, adiabaticHour.BuildingHeatingLoadW);
        AssertClose(independentHour.BuildingCoolingLoadW, adiabaticHour.BuildingCoolingLoadW);
    }

    [Fact]
    public void Fixtures_RemainDeterministicAcrossRepeatedRuns()
    {
        var service = CreateService();
        var input = LoadFixture("two-zone-interzone-conductance").Input;

        var first = service.Simulate(input);
        var second = service.Simulate(input);

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        AssertEquivalentResults(first, second);
    }

    [Fact]
    public void FixtureOutputs_BuildingTotalsEqualZoneSums()
    {
        var service = CreateService();
        var fixtures = LoadAllFixtures();

        foreach (var fixture in fixtures)
        {
            var result = service.Simulate(fixture.Input);
            Assert.True(result.IsValid);

            foreach (var hour in result.HourlyResults)
            {
                AssertClose(hour.HeatingLoadsByZoneW.Values.Sum(), hour.BuildingHeatingLoadW);
                AssertClose(hour.CoolingLoadsByZoneW.Values.Sum(), hour.BuildingCoolingLoadW);
            }

            AssertClose(
                result.AnnualSummary.AnnualHeatingEnergyByZoneKWh.Values.Sum(),
                result.AnnualSummary.AnnualHeatingEnergyTotalKWh);
            AssertClose(
                result.AnnualSummary.AnnualCoolingEnergyByZoneKWh.Values.Sum(),
                result.AnnualSummary.AnnualCoolingEnergyTotalKWh);

            foreach (var month in result.MonthlySummariesOrEmpty)
            {
                AssertClose(month.HeatingEnergyByZoneKWh.Values.Sum(), month.BuildingHeatingEnergyKWh);
                AssertClose(month.CoolingEnergyByZoneKWh.Values.Sum(), month.BuildingCoolingEnergyKWh);
            }
        }
    }

    private static Iso52016MultiZoneEnergySimulationService CreateService()
    {
        var validator = new Iso52016MultiZoneInputValidator();
        var graphBuilder = new Iso52016MultiZoneGraphBuilder(validator);
        var solver = new Iso52016MultiZoneHourlySolver();
        return new Iso52016MultiZoneEnergySimulationService(validator, graphBuilder, solver);
    }

    private static IReadOnlyList<MultiZoneFixtureDocument> LoadAllFixtures()
    {
        var directory = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "multi-zone");
        var files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);

        return files
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(LoadFixtureFromPath)
            .ToArray();
    }

    private static MultiZoneFixtureDocument LoadFixture(string id)
    {
        var path = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "multi-zone", $"{id}.json");
        return LoadFixtureFromPath(path);
    }

    private static MultiZoneFixtureDocument LoadFixtureFromPath(string path)
    {
        var json = File.ReadAllText(path);
        var fixture = JsonSerializer.Deserialize<MultiZoneFixtureDocument>(json, JsonOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Failed to deserialize multi-zone fixture '{path}'.");

        if (fixture.Input is null)
            throw new InvalidOperationException($"Fixture '{path}' does not contain input.");

        if (fixture.ClaimBoundary is null)
            throw new InvalidOperationException($"Fixture '{path}' does not contain claimBoundary.");

        return fixture;
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record MultiZoneFixtureDocument(
        string Id,
        IReadOnlyList<string> ClaimBoundary,
        string? Notes,
        MultiZoneCalculationInput Input);
}
