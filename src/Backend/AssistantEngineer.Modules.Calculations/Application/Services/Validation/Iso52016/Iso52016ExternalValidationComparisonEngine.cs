using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

public sealed class Iso52016ExternalValidationComparisonEngine
{
    public Iso52016ExternalValidationComparisonResult Compare(
        string fixtureId,
        Iso52016ExternalValidationExpectedResult expected,
        Iso52016ExternalValidationExpectedResult actual,
        Iso52016ExternalValidationTolerance tolerance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(tolerance);

        var deltas = new List<Iso52016ExternalValidationDelta>();
        CompareScalar("AnnualHeatingKWh", expected.AnnualHeatingKWh, actual.AnnualHeatingKWh, tolerance, deltas);
        CompareScalar("AnnualCoolingKWh", expected.AnnualCoolingKWh, actual.AnnualCoolingKWh, tolerance, deltas);
        CompareScalar("PeakHeatingW", expected.PeakHeatingW, actual.PeakHeatingW, tolerance, deltas);
        CompareScalar("PeakCoolingW", expected.PeakCoolingW, actual.PeakCoolingW, tolerance, deltas);
        CompareScalar("MeanOperativeTemperatureC", expected.MeanOperativeTemperatureC, actual.MeanOperativeTemperatureC, tolerance, deltas);
        CompareScalar("MaxOperativeTemperatureC", expected.MaxOperativeTemperatureC, actual.MaxOperativeTemperatureC, tolerance, deltas);
        CompareScalar("MinOperativeTemperatureC", expected.MinOperativeTemperatureC, actual.MinOperativeTemperatureC, tolerance, deltas);

        if (expected.HourlyResultCount.HasValue || actual.HourlyResultCount.HasValue)
        {
            CompareScalar(
                "HourlyResultCount",
                expected.HourlyResultCount,
                actual.HourlyResultCount,
                tolerance,
                deltas);
        }

        CompareSeries("MonthlyHeatingKWh", expected.MonthlyHeatingKWh, actual.MonthlyHeatingKWh, tolerance, deltas);
        CompareSeries("MonthlyCoolingKWh", expected.MonthlyCoolingKWh, actual.MonthlyCoolingKWh, tolerance, deltas);

        var failedMetrics = deltas
            .Where(delta => delta.Status == Iso52016ExternalValidationStatus.Failed)
            .Select(delta => delta.MetricName)
            .ToArray();

        return new Iso52016ExternalValidationComparisonResult(
            FixtureId: fixtureId,
            Status: failedMetrics.Length == 0 ? Iso52016ExternalValidationStatus.Passed : Iso52016ExternalValidationStatus.Failed,
            Deltas: deltas,
            FailedMetrics: failedMetrics);
    }

    private static void CompareSeries(
        string metricName,
        IReadOnlyList<double>? expected,
        IReadOnlyList<double>? actual,
        Iso52016ExternalValidationTolerance tolerance,
        ICollection<Iso52016ExternalValidationDelta> deltas)
    {
        if (expected is null && actual is null)
            return;

        if (expected is null || actual is null)
        {
            deltas.Add(
                BuildMissingDelta(
                    metricName,
                    expected is null ? "expected series missing" : "actual series missing",
                    tolerance));
            return;
        }

        if (expected.Count != actual.Count)
        {
            deltas.Add(
                BuildCountMismatchDelta(metricName, expected.Count, actual.Count, tolerance));
        }

        var count = Math.Min(expected.Count, actual.Count);
        for (var i = 0; i < count; i++)
        {
            CompareScalar(
                $"{metricName}[{i + 1}]",
                expected[i],
                actual[i],
                tolerance,
                deltas);
        }
    }

    private static void CompareScalar(
        string metricName,
        double? expectedValue,
        double? actualValue,
        Iso52016ExternalValidationTolerance tolerance,
        ICollection<Iso52016ExternalValidationDelta> deltas)
    {
        if (!expectedValue.HasValue && !actualValue.HasValue)
            return;

        if (!expectedValue.HasValue || !actualValue.HasValue)
        {
            deltas.Add(
                BuildMissingDelta(
                    metricName,
                    expectedValue.HasValue ? "actual value missing" : "expected value missing",
                    tolerance));
            return;
        }

        var absoluteDelta = Math.Abs(actualValue.Value - expectedValue.Value);
        var relativeDeltaPercent = Math.Abs(expectedValue.Value) < double.Epsilon
            ? (Math.Abs(actualValue.Value) < double.Epsilon ? 0.0 : 100.0)
            : absoluteDelta / Math.Abs(expectedValue.Value) * 100.0;

        var withinAbsolute = absoluteDelta <= tolerance.AbsoluteTolerance;
        var withinRelative = relativeDeltaPercent <= tolerance.RelativeTolerancePercent;
        var passed = withinAbsolute || withinRelative;

        var status = passed
            ? Iso52016ExternalValidationStatus.Passed
            : Iso52016ExternalValidationStatus.Failed;

        var diagnostics = passed
            ? "Within tolerance."
            : $"Out of tolerance. absolute={absoluteDelta:0.######}, relative={relativeDeltaPercent:0.######}%";

        deltas.Add(
            new Iso52016ExternalValidationDelta(
                MetricName: metricName,
                ExpectedValue: expectedValue.Value,
                ActualValue: actualValue.Value,
                AbsoluteDelta: absoluteDelta,
                RelativeDeltaPercent: relativeDeltaPercent,
                AbsoluteTolerance: tolerance.AbsoluteTolerance,
                RelativeTolerancePercent: tolerance.RelativeTolerancePercent,
                Status: status,
                Diagnostics: diagnostics));
    }

    private static Iso52016ExternalValidationDelta BuildMissingDelta(
        string metricName,
        string diagnostics,
        Iso52016ExternalValidationTolerance tolerance) =>
        new(
            MetricName: metricName,
            ExpectedValue: double.NaN,
            ActualValue: double.NaN,
            AbsoluteDelta: double.NaN,
            RelativeDeltaPercent: double.NaN,
            AbsoluteTolerance: tolerance.AbsoluteTolerance,
            RelativeTolerancePercent: tolerance.RelativeTolerancePercent,
            Status: Iso52016ExternalValidationStatus.Failed,
            Diagnostics: diagnostics);

    private static Iso52016ExternalValidationDelta BuildCountMismatchDelta(
        string metricName,
        int expectedCount,
        int actualCount,
        Iso52016ExternalValidationTolerance tolerance) =>
        new(
            MetricName: $"{metricName}.Count",
            ExpectedValue: expectedCount,
            ActualValue: actualCount,
            AbsoluteDelta: Math.Abs(actualCount - expectedCount),
            RelativeDeltaPercent: expectedCount == 0 ? 100.0 : Math.Abs(actualCount - expectedCount) / (double)expectedCount * 100.0,
            AbsoluteTolerance: tolerance.AbsoluteTolerance,
            RelativeTolerancePercent: tolerance.RelativeTolerancePercent,
            Status: Iso52016ExternalValidationStatus.Failed,
            Diagnostics: "Series lengths are different.");
}
