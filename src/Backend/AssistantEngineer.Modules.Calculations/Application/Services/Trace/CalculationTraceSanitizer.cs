using System.Collections;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Trace;

public sealed class CalculationTraceSanitizer : ICalculationTraceSanitizer
{
    public CalculationTraceDocument Sanitize(
        CalculationTraceDocument trace,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard,
        int maxCollectionItems = 24)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (maxCollectionItems < 1)
            maxCollectionItems = 1;

        var sanitizedSteps = trace.Steps
            .Select(step => SanitizeStep(step, detailLevel, maxCollectionItems))
            .OrderBy(step => step.Sequence)
            .ToArray();

        var assumptions = NormalizeStrings(trace.Assumptions);
        var warnings = NormalizeStrings(trace.Warnings);
        var diagnostics = NormalizeDiagnostics(trace.Diagnostics);
        var metadata = trace.Metadata
            .Where(item => !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(
                item => item.Key.Trim(),
                item => item.Value.Trim(),
                StringComparer.Ordinal);

        var flattenedSteps = Flatten(sanitizedSteps).ToArray();
        var moduleKinds = flattenedSteps
            .Select(step => step.ModuleKind)
            .Distinct()
            .OrderBy(kind => kind)
            .ToArray();

        var summary = trace.Summary with
        {
            StepCount = sanitizedSteps.Length == 0 ? 0 : flattenedSteps.Length,
            Modules = moduleKinds.Length == 0 ? [trace.RootModule] : moduleKinds,
            AssumptionCount = assumptions.Count + flattenedSteps.Sum(step => step.Assumptions.Count),
            WarningCount = warnings.Count + flattenedSteps.Sum(step => step.Warnings.Count),
            DiagnosticCount = diagnostics.Count + flattenedSteps.Sum(step => step.Diagnostics.Count)
        };

        return trace with
        {
            Assumptions = assumptions,
            Warnings = warnings,
            Diagnostics = diagnostics,
            Steps = sanitizedSteps,
            Metadata = metadata,
            Summary = summary
        };
    }

    private CalculationTraceStep SanitizeStep(
        CalculationTraceStep step,
        CalculationTraceDetailLevel detailLevel,
        int maxCollectionItems)
    {
        var inputValues = detailLevel >= CalculationTraceDetailLevel.Standard
            ? SanitizeValues(step.InputValues, detailLevel, maxCollectionItems)
            : [];

        var intermediateValues = detailLevel >= CalculationTraceDetailLevel.Detailed
            ? SanitizeValues(step.IntermediateValues, detailLevel, maxCollectionItems)
            : [];

        var outputValues = detailLevel >= CalculationTraceDetailLevel.Summary
            ? SanitizeValues(step.OutputValues, detailLevel, maxCollectionItems)
            : [];

        var assumptions = detailLevel >= CalculationTraceDetailLevel.Standard
            ? NormalizeStrings(step.Assumptions)
            : [];

        var warnings = detailLevel >= CalculationTraceDetailLevel.Standard
            ? NormalizeStrings(step.Warnings)
            : [];

        var diagnostics = detailLevel >= CalculationTraceDetailLevel.Summary
            ? NormalizeDiagnostics(step.Diagnostics)
            : [];

        var childSteps = step.ChildSteps
            .Select(child => SanitizeStep(child, detailLevel, maxCollectionItems))
            .OrderBy(child => child.Sequence)
            .ToArray();

        return step with
        {
            InputValues = inputValues,
            IntermediateValues = intermediateValues,
            OutputValues = outputValues,
            Assumptions = assumptions,
            Warnings = warnings,
            Diagnostics = diagnostics,
            ChildSteps = childSteps
        };
    }

    private static IReadOnlyList<CalculationTraceValue> SanitizeValues(
        IReadOnlyList<CalculationTraceValue> values,
        CalculationTraceDetailLevel detailLevel,
        int maxCollectionItems)
    {
        var sanitized = new List<CalculationTraceValue>(values.Count);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value.Key) || string.IsNullOrWhiteSpace(value.Label))
                continue;

            var tags = value.Tags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(tag => tag, StringComparer.Ordinal)
                .ToArray();

            var sanitizedValue = value with
            {
                Key = value.Key.Trim(),
                Label = value.Label.Trim(),
                Source = string.IsNullOrWhiteSpace(value.Source) ? null : value.Source.Trim(),
                DisplayFormat = string.IsNullOrWhiteSpace(value.DisplayFormat) ? null : value.DisplayFormat.Trim(),
                Unit = value.Unit is null
                    ? null
                    : value.Unit with
                    {
                        Symbol = value.Unit.Symbol.Trim(),
                        QuantityKind = string.IsNullOrWhiteSpace(value.Unit.QuantityKind) ? null : value.Unit.QuantityKind.Trim(),
                        DisplayName = string.IsNullOrWhiteSpace(value.Unit.DisplayName) ? null : value.Unit.DisplayName.Trim()
                    },
                Tags = tags,
                Value = SanitizeValueObject(value.Value, detailLevel, maxCollectionItems)
            };

            sanitized.Add(sanitizedValue);
        }

        return sanitized
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ThenBy(item => item.Label, StringComparer.Ordinal)
            .ToArray();
    }

    private static object? SanitizeValueObject(
        object? value,
        CalculationTraceDetailLevel detailLevel,
        int maxCollectionItems)
    {
        if (value is null)
            return null;

        if (value is string text)
        {
            var trimmed = text.Trim();
            if (LooksLikeSensitivePath(trimmed))
                return "[redacted-path]";

            return trimmed;
        }

        if (value is double doubleValue)
            return Math.Round(doubleValue, 6);

        if (value is float floatValue)
            return Math.Round(floatValue, 6);

        if (value is decimal decimalValue)
            return Math.Round(decimalValue, 6);

        if (value is IEnumerable enumerable and not IDictionary)
        {
            var items = enumerable.Cast<object?>().ToArray();
            if (detailLevel == CalculationTraceDetailLevel.Summary && items.Length > maxCollectionItems)
                return SummarizeCollection(items);

            return items.Select(item => SanitizeValueObject(item, detailLevel, maxCollectionItems)).ToArray();
        }

        return value;
    }

    private static CalculationTraceArraySummary SummarizeCollection(
        IReadOnlyList<object?> values)
    {
        var numeric = values
            .Where(item => item is not null)
            .Select(TryConvertDouble)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToArray();

        return numeric.Length == 0
            ? new CalculationTraceArraySummary(
                Count: values.Count,
                First: values.FirstOrDefault(),
                Last: values.LastOrDefault(),
                Min: null,
                Max: null)
            : new CalculationTraceArraySummary(
                Count: values.Count,
                First: Math.Round(numeric.First(), 6),
                Last: Math.Round(numeric.Last(), 6),
                Min: Math.Round(numeric.Min(), 6),
                Max: Math.Round(numeric.Max(), 6));
    }

    private static double? TryConvertDouble(
        object? value)
    {
        if (value is null)
            return null;

        return value switch
        {
            double item => item,
            float item => item,
            decimal item => (double)item,
            int item => item,
            long item => item,
            _ => null
        };
    }

    private static IReadOnlyList<string> NormalizeStrings(
        IReadOnlyList<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<CalculationTraceDiagnostic> NormalizeDiagnostics(
        IReadOnlyList<CalculationTraceDiagnostic> diagnostics) =>
        diagnostics
            .Where(diagnostic =>
                !string.IsNullOrWhiteSpace(diagnostic.Code) &&
                !string.IsNullOrWhiteSpace(diagnostic.Message))
            .Select(diagnostic => diagnostic with
            {
                Code = diagnostic.Code.Trim(),
                Message = diagnostic.Message.Trim(),
                Context = string.IsNullOrWhiteSpace(diagnostic.Context) ? null : diagnostic.Context.Trim(),
                Source = string.IsNullOrWhiteSpace(diagnostic.Source) ? null : diagnostic.Source.Trim()
            })
            .Distinct()
            .OrderByDescending(diagnostic => SeverityRank(diagnostic.Severity))
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Context, StringComparer.Ordinal)
            .ToArray();

    private static int SeverityRank(
        CalculationTraceSeverity severity) =>
        severity switch
        {
            CalculationTraceSeverity.Error => 4,
            CalculationTraceSeverity.Warning => 3,
            CalculationTraceSeverity.Assumption => 2,
            CalculationTraceSeverity.Info => 1,
            _ => 0
        };

    private static IEnumerable<CalculationTraceStep> Flatten(
        IEnumerable<CalculationTraceStep> steps)
    {
        foreach (var step in steps)
        {
            yield return step;

            foreach (var child in Flatten(step.ChildSteps))
            {
                yield return child;
            }
        }
    }

    private static bool LooksLikeSensitivePath(
        string value)
    {
        if (value.Length < 3)
            return false;

        if (value.StartsWith(@"\\", StringComparison.Ordinal) ||
            value.StartsWith("/", StringComparison.Ordinal))
            return true;

        return char.IsLetter(value[0]) &&
               value[1] == ':' &&
               (value[2] == '\\' || value[2] == '/');
    }
}
