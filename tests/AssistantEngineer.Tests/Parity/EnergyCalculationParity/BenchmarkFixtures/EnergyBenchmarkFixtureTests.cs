using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.BenchmarkFixtures;

public class EnergyBenchmarkFixtureTests
{
    [Fact]
    public void BenchmarkFixtureLoader_LoadsActiveFixtures()
    {
        var result = EnergyBenchmarkFixtureLoader.LoadFromDefaultDirectory();
        var fixtureNames = result.Fixtures
            .Select(fixture => fixture.FixtureName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("annual-constant-heating-8760", fixtureNames);
        Assert.Contains("annual-constant-cooling-8760", fixtureNames);
        Assert.Contains("signed-component-balance-winter", fixtureNames);
        Assert.Contains("signed-component-balance-summer", fixtureNames);
        Assert.Contains("signed-component-balance-with-infiltration-winter", fixtureNames);
        Assert.All(result.Fixtures, fixture =>
        {
            Assert.Equal("Active", fixture.Status);
            Assert.NotEmpty(fixture.FixtureName);
            Assert.NotEmpty(fixture.Description);
            Assert.NotEmpty(fixture.Category);
            Assert.NotEmpty(fixture.ReferenceType);
            Assert.NotEmpty(fixture.Method);
            Assert.NotNull(fixture.Input);
            Assert.NotNull(fixture.Tolerances);
            Assert.NotEmpty(fixture.Assumptions);
            Assert.NotEmpty(fixture.Notes);
        });
    }

    [Fact]
    public void BenchmarkFixtureLoader_SkipsPendingAndDisabled()
    {
        var directory = CreateTempDirectory();

        try
        {
            WriteFixture(directory, "active.json", CreateFixtureJson("active-fixture", "Active"));
            WriteFixture(directory, "pending.json", CreateFixtureJson("pending-fixture", "Pending"));
            WriteFixture(directory, "disabled.json", CreateFixtureJson("disabled-fixture", "Disabled"));

            var result = EnergyBenchmarkFixtureLoader.LoadFromDirectory(directory);

            var active = Assert.Single(result.Fixtures);
            Assert.Equal("active-fixture", active.FixtureName);
            Assert.Equal(2, result.SkippedFixtures.Count);
            Assert.Contains(result.SkippedFixtures, skipped =>
                skipped.FixtureName == "pending-fixture" &&
                skipped.Status == "Pending" &&
                skipped.Reason.Contains("skipped by default", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.SkippedFixtures, skipped =>
                skipped.FixtureName == "disabled-fixture" &&
                skipped.Status == "Disabled" &&
                skipped.Reason.Contains("skipped by default", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void BenchmarkFixtureLoader_InvalidFixtureFailsWithHelpfulMessage()
    {
        var directory = CreateTempDirectory();

        try
        {
            WriteFixture(directory, "invalid.json", "{ invalid json");

            var exception = Assert.Throws<InvalidOperationException>(() =>
                EnergyBenchmarkFixtureLoader.LoadFromDirectory(directory));

            Assert.Contains("JSON is invalid", exception.Message);
            Assert.Contains("invalid.json", exception.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void BenchmarkComparison_PassesWithinAbsoluteTolerance()
    {
        var result = EnergyBenchmarkComparison.CompareNumeric(
            "fixture",
            "annualHeatingDemandKWh",
            expected: 0,
            actual: 0.005,
            new EnergyBenchmarkTolerance { Absolute = 0.01 });

        Assert.True(result.Passed, result.Message);
        Assert.Equal(0.005, result.AbsoluteDifference, precision: 6);
    }

    [Fact]
    public void BenchmarkComparison_PassesWithinRelativeTolerance()
    {
        var result = EnergyBenchmarkComparison.CompareNumeric(
            "fixture",
            "annualHeatingDemandKWh",
            expected: 100,
            actual: 100.05,
            new EnergyBenchmarkTolerance
            {
                Absolute = 0.001,
                RelativePercent = 0.1
            });

        Assert.True(result.Passed, result.Message);
        Assert.True(result.AbsoluteDifference > result.AbsoluteTolerance);
        Assert.True(result.RelativeDifferencePercent <= result.RelativeTolerancePercent);
    }

    [Fact]
    public void BenchmarkComparison_FailsOutsideTolerance()
    {
        var result = EnergyBenchmarkComparison.CompareNumeric(
            "fixture",
            "annualHeatingDemandKWh",
            expected: 100,
            actual: 102,
            new EnergyBenchmarkTolerance
            {
                Absolute = 0.1,
                RelativePercent = 1
            });

        Assert.False(result.Passed);
        Assert.Contains("fixture", result.Message);
        Assert.Contains("annualHeatingDemandKWh", result.Message);
    }

    [Fact]
    public void AnnualEnergyBenchmarkFixtures_AllActiveFixturesPass()
    {
        var loadResult = EnergyBenchmarkFixtureLoader.LoadFromDefaultDirectory();
        var engine = new AnnualEnergyBalanceEngine();
        var failures = new List<string>();

        foreach (var fixture in loadResult.Fixtures)
        {
            if (fixture.Category is not ("AnnualEnergyBalance" or "SignedComponentBalance"))
            {
                failures.Add($"Fixture '{fixture.FixtureName}' has unsupported benchmark category '{fixture.Category}'.");
                continue;
            }

            var input = CreateAnnualEnergyInput(fixture);
            var result = engine.Calculate(input);

            if (!result.IsSuccess)
            {
                failures.Add(WithFixtureContext(fixture, result.Error));
                continue;
            }

            failures.AddRange(EnergyBenchmarkComparison
                .CompareExpectedNumericFields(fixture, result.Value)
                .Where(comparison => !comparison.Passed)
                .Select(comparison => WithFixtureContext(fixture, comparison.Message)));

            foreach (var expectedBoolean in EnergyBenchmarkComparison.GetExpectedBooleanValues(fixture))
            {
                if (!EnergyBenchmarkComparison.TryGetBooleanValue(
                        result.Value,
                        expectedBoolean.Key,
                        out var actual,
                        out var failure))
                {
                    failures.Add(WithFixtureContext(
                        fixture,
                        $"Fixture '{fixture.FixtureName}' field '{expectedBoolean.Key}' could not be read from actual result: {failure}"));
                    continue;
                }

                if (actual != expectedBoolean.Value)
                {
                    failures.Add(WithFixtureContext(
                        fixture,
                        $"Fixture '{fixture.FixtureName}' field '{expectedBoolean.Key}' failed: expected {expectedBoolean.Value}, actual {actual}."));
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static AnnualEnergyBalanceInput CreateAnnualEnergyInput(EnergyBenchmarkFixture fixture)
    {
        var input = fixture.Input;

        if (!string.Equals(input.HourlyPattern, "constant", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Fixture '{fixture.FixtureName}' uses unsupported hourlyPattern '{input.HourlyPattern}'.");
        }

        var values = input.HourlyValues;
        var hours = Enumerable
            .Range(0, input.HourlyRecordCount)
            .Select(hourIndex => new AnnualEnergyBalanceHourInput(
                HourIndex: hourIndex,
                Month: ResolveMonth(hourIndex),
                HeatingLoadW: values.HeatingLoadW,
                CoolingLoadW: values.CoolingLoadW,
                TransmissionW: values.TransmissionW,
                VentilationW: values.VentilationW,
                InfiltrationW: values.InfiltrationW,
                SolarGainsW: values.SolarGainsW,
                InternalGainsW: values.InternalGainsW,
                GroundW: values.GroundW,
                HourDurationH: values.HourDurationH,
                TransmissionBalanceW: values.TransmissionBalanceW,
                VentilationBalanceW: values.VentilationBalanceW,
                InfiltrationBalanceW: values.InfiltrationBalanceW,
                GroundBalanceW: values.GroundBalanceW))
            .ToArray();

        return new AnnualEnergyBalanceInput(
            input.BuildingId,
            input.BuildingName,
            input.BuildingAreaM2,
            input.Year,
            hours,
            DiagnosticsContext: fixture.FixtureName,
            EnergyDataSource: input.EnergyDataSource,
            IsTrueHourly8760: input.IsTrueHourly8760,
            ActualMethod: fixture.Method);
    }

    private static int ResolveMonth(int hourIndex)
    {
        var monthHours = new[]
        {
            744,
            672,
            744,
            720,
            744,
            720,
            744,
            744,
            720,
            744,
            720,
            744
        };

        var remaining = hourIndex;
        for (var month = 0; month < monthHours.Length; month++)
        {
            if (remaining < monthHours[month])
                return month + 1;

            remaining -= monthHours[month];
        }

        return 12;
    }

    private static string WithFixtureContext(EnergyBenchmarkFixture fixture, string message) =>
        $"{message} Assumptions: {string.Join(" | ", fixture.Assumptions)}. Notes: {string.Join(" | ", fixture.Notes)}.";

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "assistant-engineer-benchmark-fixtures",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteFixture(string directory, string fileName, string content) =>
        File.WriteAllText(Path.Combine(directory, fileName), content);

    private static string CreateFixtureJson(string fixtureName, string status) =>
        $$"""
        {
          "fixtureName": "{{fixtureName}}",
          "description": "Temporary loader fixture.",
          "category": "AnnualEnergyBalance",
          "referenceType": "BenchmarkReference",
          "method": "Energy Calculation Parity / Annual 8760 Energy Balance",
          "status": "{{status}}",
          "input": {
            "calculationBasis": "annual-energy-balance",
            "buildingAreaM2": 100,
            "hourlyPattern": "constant",
            "hourlyRecordCount": 1,
            "hourlyValues": {
              "heatingLoadW": 1000
            }
          },
          "expected": {
            "annualHeatingDemandKWh": 1
          },
          "tolerances": {
            "defaultAbsolute": 0.000001,
            "defaultRelativePercent": 0.000001
          },
          "assumptions": [
            "Temporary fixture for loader tests."
          ],
          "notes": [
            "No external parity claim."
          ]
        }
        """;
}
