using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ManualIndependentFixtureComparisonTests
{
    private readonly Iso52016ExternalValidationFixtureLoader _loader = new();
    private readonly Iso52016ExternalValidationComparisonEngine _comparisonEngine = new();
    private readonly Iso52016ManualIndependentReferenceCalculator _manualCalculator = new();

    [Fact]
    public void ManualIndependentFixtures_CompareAndPassAgainstManualCalculator()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());

        foreach (var fixture in fixtures)
        {
            var actual = _manualCalculator.Calculate(fixture);
            var result = _comparisonEngine.Compare(
                fixture.Id,
                fixture.Expected,
                actual,
                fixture.Tolerance);

            Assert.True(result.IsSuccess, $"Fixture {fixture.Id} failed: {string.Join("; ", result.FailedMetrics)}");
            Assert.Equal(Iso52016ExternalValidationStatus.Passed, result.Status);
        }
    }

    [Fact]
    public void ManualIndependentComparison_FailsWithClearDeltaDiagnosticsForIntentionalMismatch()
    {
        var fixture = _loader
            .LoadFromDirectory(FixtureDirectory())
            .Single(item => item.Id == "manual-independent-steady-heating-simple-room");

        var actual = _manualCalculator.Calculate(fixture) with
        {
            AnnualHeatingKWh = fixture.Expected.AnnualHeatingKWh + 50.0
        };

        var result = _comparisonEngine.Compare(
            fixture.Id,
            fixture.Expected,
            actual,
            fixture.Tolerance);

        Assert.False(result.IsSuccess);
        Assert.Contains("AnnualHeatingKWh", result.FailedMetrics);

        var delta = Assert.Single(result.Deltas, item => item.MetricName == "AnnualHeatingKWh");
        Assert.Equal(Iso52016ExternalValidationStatus.Failed, delta.Status);
        Assert.Contains("Out of tolerance", delta.Diagnostics);
        Assert.True(delta.AbsoluteDelta > fixture.Tolerance.AbsoluteTolerance);
    }

    private static string FixtureDirectory() =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "external-validation");
}
