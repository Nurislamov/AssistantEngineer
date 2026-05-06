using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

public sealed class EngineeringCalculationModeComparisonEngine
{
    public EngineeringCalculationModeComparisonResult Compare(
        EngineeringCalculationModeComparisonRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<EngineeringCalculationModeDiagnostic>();
        var deltas = new List<EngineeringCalculationModeDelta>();

        var compatibility = request.CompatibilityMetrics
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);
        var inspired = request.InspiredMetrics
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

        var metricNames = compatibility.Keys
            .Union(inspired.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (var metricName in metricNames)
        {
            if (!compatibility.TryGetValue(metricName, out var compatibilityValue))
            {
                diagnostics.Add(new EngineeringCalculationModeDiagnostic(
                    "Rollup.MissingCompatibilityMetric",
                    $"Compatibility metric '{metricName}' is missing.",
                    request.DiagnosticsContext));
                continue;
            }

            if (!inspired.TryGetValue(metricName, out var inspiredValue))
            {
                diagnostics.Add(new EngineeringCalculationModeDiagnostic(
                    "Rollup.MissingInspiredMetric",
                    $"Inspired metric '{metricName}' is missing.",
                    request.DiagnosticsContext));
                continue;
            }

            var absoluteTolerance = ResolveTolerance(
                request.AbsoluteTolerances,
                metricName,
                request.DefaultAbsoluteTolerance);
            var relativeTolerance = ResolveTolerance(
                request.RelativeTolerancesPercent,
                metricName,
                request.DefaultRelativeTolerancePercent);

            var absoluteDelta = Math.Abs(inspiredValue - compatibilityValue);
            var relativeDelta = CalculateRelativeDeltaPercent(compatibilityValue, inspiredValue, out var zeroBaseline);

            var absolutePass = absoluteDelta <= absoluteTolerance;
            var relativePass = relativeDelta.HasValue && relativeDelta.Value <= relativeTolerance;
            var pass = absolutePass || relativePass;

            var warning = zeroBaseline && Math.Abs(inspiredValue) > 1e-12;
            var message = warning
                ? $"Metric '{metricName}' has zero compatibility baseline; absolute tolerance governs pass/fail."
                : pass
                    ? $"Metric '{metricName}' is within configured tolerance."
                    : $"Metric '{metricName}' exceeds configured tolerance.";

            deltas.Add(new EngineeringCalculationModeDelta(
                MetricName: metricName,
                CompatibilityValue: Round6(compatibilityValue),
                InspiredValue: Round6(inspiredValue),
                AbsoluteDelta: Round6(absoluteDelta),
                RelativeDeltaPercent: relativeDelta.HasValue ? Round6(relativeDelta.Value) : null,
                AbsoluteTolerance: Round6(absoluteTolerance),
                RelativeTolerancePercent: Round6(relativeTolerance),
                IsPass: pass,
                IsWarning: warning,
                DiagnosticMessage: message));
        }

        var hasFailures = deltas.Any(item => !item.IsPass);
        var hasWarnings = deltas.Any(item => item.IsWarning) || diagnostics.Count > 0;
        var summaryStatus = hasFailures
            ? "Fail"
            : hasWarnings ? "Warning" : "Pass";

        return new EngineeringCalculationModeComparisonResult(
            Domain: request.Domain,
            CompatibilityModeId: request.CompatibilityModeId,
            InspiredModeId: request.InspiredModeId,
            Deltas: deltas,
            IsPass: !hasFailures,
            HasWarnings: hasWarnings,
            SummaryStatus: summaryStatus,
            Diagnostics: diagnostics);
    }

    private static double ResolveTolerance(
        IReadOnlyDictionary<string, double>? source,
        string metricName,
        double fallback)
    {
        if (source is null)
            return Math.Max(0.0, fallback);

        return source.TryGetValue(metricName, out var value)
            ? Math.Max(0.0, value)
            : Math.Max(0.0, fallback);
    }

    private static double? CalculateRelativeDeltaPercent(
        double compatibilityValue,
        double inspiredValue,
        out bool zeroBaseline)
    {
        if (Math.Abs(compatibilityValue) <= 1e-12)
        {
            zeroBaseline = true;
            if (Math.Abs(inspiredValue) <= 1e-12)
                return 0.0;
            return null;
        }

        zeroBaseline = false;
        return Math.Abs(inspiredValue - compatibilityValue) / Math.Abs(compatibilityValue) * 100.0;
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
