using AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services.ExternalComparison;

internal sealed class EnergyPlusComparisonResultBuilder
{
    public ExternalComparisonResult Build(
        ExternalComparisonCase comparisonCase,
        ExternalComparisonExpectedOutput? importedExternalOutput)
    {
        if (comparisonCase.Status == ExternalComparisonStatus.Planned)
        {
            return NewResult(
                comparisonCase,
                ExternalComparisonStatus.Planned,
                null,
                "Case is planned only; no comparison output is available yet.");
        }

        if (importedExternalOutput is null)
        {
            return NewResult(
                comparisonCase,
                ExternalComparisonStatus.FixtureDefined,
                null,
                "Case fixture is defined but external comparison output is not imported.");
        }

        if (comparisonCase.Provenance is null || string.IsNullOrWhiteSpace(comparisonCase.Provenance.SourceTool))
        {
            return NewResult(
                comparisonCase,
                ExternalComparisonStatus.NotAValidationClaim,
                null,
                "External output is present but provenance is missing.");
        }

        if (comparisonCase.Tolerance is null || comparisonCase.ExpectedOutput is null)
        {
            return NewResult(
                comparisonCase,
                ExternalComparisonStatus.ExternalOutputImported,
                null,
                "External output imported; expected output and tolerance are required for comparison.");
        }

        var expected = comparisonCase.ExpectedOutput.Metrics;
        var actual = importedExternalOutput.Metrics;

        if (expected.Count == 0 || actual.Count == 0)
        {
            return NewResult(
                comparisonCase,
                ExternalComparisonStatus.Compared,
                false,
                "Comparison metadata is present, but metrics are incomplete.");
        }

        var tolerance = comparisonCase.Tolerance;
        var allPassed = true;
        var diagnostics = new List<string>();

        foreach (var metric in expected)
        {
            if (!actual.TryGetValue(metric.Key, out var actualValue))
            {
                allPassed = false;
                diagnostics.Add($"Missing metric in imported output: {metric.Key}.");
                continue;
            }

            var absoluteDelta = Math.Abs(actualValue - metric.Value);
            var relativeDelta = Math.Abs(metric.Value) < 0.0000001
                ? absoluteDelta
                : absoluteDelta / Math.Abs(metric.Value) * 100.0;
            var allowedDelta = Math.Max(tolerance.Absolute, Math.Abs(metric.Value) * tolerance.RelativePercent / 100.0);

            if (absoluteDelta > allowedDelta && relativeDelta > tolerance.RelativePercent)
            {
                allPassed = false;
                diagnostics.Add(
                    $"Metric '{metric.Key}' is outside tolerance. Expected={metric.Value}, Actual={actualValue}, |delta|={absoluteDelta}.");
            }
        }

        return new ExternalComparisonResult
        {
            CaseId = comparisonCase.CaseId,
            Status = allPassed ? ExternalComparisonStatus.PassedTolerance : ExternalComparisonStatus.FailedTolerance,
            PassedTolerance = allPassed,
            ActualMetrics = actual,
            ExpectedMetrics = expected,
            Diagnostics = diagnostics.Count == 0 ? ["Compared with documented tolerance."] : diagnostics
        };
    }

    private static ExternalComparisonResult NewResult(
        ExternalComparisonCase comparisonCase,
        ExternalComparisonStatus status,
        bool? passedTolerance,
        string diagnostic) =>
        new()
        {
            CaseId = comparisonCase.CaseId,
            Status = status,
            PassedTolerance = passedTolerance,
            ActualMetrics = new Dictionary<string, double>(),
            ExpectedMetrics = comparisonCase.ExpectedOutput?.Metrics ?? new Dictionary<string, double>(),
            Diagnostics = [diagnostic]
        };
}
