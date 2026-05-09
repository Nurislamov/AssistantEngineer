using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Trace;

public sealed class CalculationTraceBuilder : ICalculationTraceBuilder
{
    private const string SchemaVersion = "1.0";

    private readonly Dictionary<string, MutableStep> _stepsById = new(StringComparer.Ordinal);
    private readonly List<string> _rootStepIds = [];
    private readonly List<string> _documentAssumptions = [];
    private readonly List<string> _documentWarnings = [];
    private readonly List<CalculationTraceDiagnostic> _documentDiagnostics = [];
    private readonly Dictionary<string, string> _metadata = new(StringComparer.Ordinal);

    private bool _isInitialized;
    private int _sequence;
    private string _traceId = "trace-uninitialized";
    private string? _calculationId;
    private string _calculationType = "Unknown";
    private DateTimeOffset? _createdTimestampUtc;
    private CalculationTraceModuleKind _rootModule = CalculationTraceModuleKind.Generic;

    public CalculationTraceDetailLevel DetailLevel { get; private set; } = CalculationTraceDetailLevel.Standard;

    public void SetDetailLevel(
        CalculationTraceDetailLevel detailLevel)
    {
        DetailLevel = detailLevel;
    }

    public void Initialize(
        string traceId,
        string calculationType,
        CalculationTraceModuleKind rootModule,
        string? calculationId = null,
        DateTimeOffset? createdTimestampUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(calculationType);

        _stepsById.Clear();
        _rootStepIds.Clear();
        _documentAssumptions.Clear();
        _documentWarnings.Clear();
        _documentDiagnostics.Clear();
        _metadata.Clear();
        _sequence = 0;

        _traceId = traceId.Trim();
        _calculationType = calculationType.Trim();
        _rootModule = rootModule;
        _calculationId = string.IsNullOrWhiteSpace(calculationId) ? null : calculationId.Trim();
        _createdTimestampUtc = createdTimestampUtc;
        _isInitialized = true;

        if (metadata is null)
            return;

        foreach (var pair in metadata.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                continue;

            _metadata[pair.Key.Trim()] = pair.Value.Trim();
        }
    }

    public string AddStep(
        CalculationTraceModuleKind moduleKind,
        string stepName,
        string? formulaOrConventionLabel = null,
        string? parentStepId = null,
        string? stepId = null,
        double? durationMilliseconds = null)
    {
        EnsureInitialized();

        if (DetailLevel == CalculationTraceDetailLevel.None)
            return string.Empty;

        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);

        var resolvedStepId = ResolveStepId(stepId);
        var newStep = new MutableStep(
            resolvedStepId,
            moduleKind,
            stepName.Trim(),
            ++_sequence,
            string.IsNullOrWhiteSpace(formulaOrConventionLabel) ? null : formulaOrConventionLabel.Trim(),
            durationMilliseconds);

        if (!string.IsNullOrWhiteSpace(parentStepId) && _stepsById.TryGetValue(parentStepId, out var parentStep))
        {
            parentStep.ChildStepIds.Add(resolvedStepId);
        }
        else
        {
            _rootStepIds.Add(resolvedStepId);
        }

        _stepsById.Add(resolvedStepId, newStep);
        return resolvedStepId;
    }

    public void AddInputValue(string stepId, CalculationTraceValue value)
    {
        if (!CanCaptureInputs())
            return;

        AddStepValue(stepId, value, ValueTarget.Input);
    }

    public void AddIntermediateValue(string stepId, CalculationTraceValue value)
    {
        if (!CanCaptureIntermediateValues())
            return;

        AddStepValue(stepId, value, ValueTarget.Intermediate);
    }

    public void AddOutputValue(string stepId, CalculationTraceValue value)
    {
        if (!CanCaptureOutputs())
            return;

        AddStepValue(stepId, value, ValueTarget.Output);
    }

    public void AddAssumption(
        string stepId,
        string assumption)
    {
        if (!CanCaptureAssumptionsAndWarnings() || string.IsNullOrWhiteSpace(assumption))
            return;

        if (!_stepsById.TryGetValue(stepId, out var step))
            return;

        step.Assumptions.Add(assumption.Trim());
    }

    public void AddWarning(
        string stepId,
        string warning)
    {
        if (!CanCaptureAssumptionsAndWarnings() || string.IsNullOrWhiteSpace(warning))
            return;

        if (!_stepsById.TryGetValue(stepId, out var step))
            return;

        step.Warnings.Add(warning.Trim());
    }

    public void AddDiagnostic(
        string stepId,
        CalculationTraceDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        if (!CanCaptureDiagnostics() || !_stepsById.TryGetValue(stepId, out var step))
            return;

        step.Diagnostics.Add(diagnostic);
    }

    public void AddDocumentAssumption(
        string assumption)
    {
        if (!CanCaptureAssumptionsAndWarnings() || string.IsNullOrWhiteSpace(assumption))
            return;

        _documentAssumptions.Add(assumption.Trim());
    }

    public void AddDocumentWarning(
        string warning)
    {
        if (!CanCaptureAssumptionsAndWarnings() || string.IsNullOrWhiteSpace(warning))
            return;

        _documentWarnings.Add(warning.Trim());
    }

    public void AddDocumentDiagnostic(
        CalculationTraceDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        if (!CanCaptureDiagnostics())
            return;

        _documentDiagnostics.Add(diagnostic);
    }

    public void Merge(
        CalculationTraceDocument document,
        string? parentStepId = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        EnsureInitialized();

        if (DetailLevel == CalculationTraceDetailLevel.None)
            return;

        var parentResolved = string.IsNullOrWhiteSpace(parentStepId)
            ? null
            : (_stepsById.ContainsKey(parentStepId) ? parentStepId : null);

        foreach (var step in document.Steps.OrderBy(item => item.Sequence))
        {
            MergeStep(step, parentResolved);
        }

        foreach (var assumption in document.Assumptions)
        {
            AddDocumentAssumption(assumption);
        }

        foreach (var warning in document.Warnings)
        {
            AddDocumentWarning(warning);
        }

        foreach (var diagnostic in document.Diagnostics)
        {
            AddDocumentDiagnostic(diagnostic);
        }
    }

    public CalculationTraceDocument Build()
    {
        EnsureInitialized();

        if (DetailLevel == CalculationTraceDetailLevel.None)
        {
            return new CalculationTraceDocument(
                TraceId: _traceId,
                CalculationId: _calculationId,
                CalculationType: _calculationType,
                CreatedTimestampUtc: _createdTimestampUtc,
                RootModule: _rootModule,
                Steps: [],
                Summary: new CalculationTraceSummary(0, 0, 0, 0, [_rootModule]),
                Assumptions: [],
                Warnings: [],
                Diagnostics: [],
                Metadata: _metadata,
                SchemaVersion: SchemaVersion);
        }

        var orderedRootSteps = _rootStepIds
            .Select(stepId => _stepsById[stepId])
            .OrderBy(step => step.Sequence)
            .Select(BuildStep)
            .ToArray();

        var assumptions = DistinctStrings(_documentAssumptions);
        var warnings = DistinctStrings(_documentWarnings);
        var diagnostics = DistinctDiagnostics(_documentDiagnostics);

        var allSteps = FlattenSteps(orderedRootSteps).ToArray();
        var allStepDiagnostics = allSteps
            .SelectMany(step => step.Diagnostics)
            .ToArray();
        var allStepAssumptions = allSteps
            .SelectMany(step => step.Assumptions)
            .ToArray();
        var allStepWarnings = allSteps
            .SelectMany(step => step.Warnings)
            .ToArray();

        var moduleSet = allSteps
            .Select(step => step.ModuleKind)
            .Distinct()
            .OrderBy(kind => kind)
            .ToArray();

        var summary = new CalculationTraceSummary(
            StepCount: allSteps.Length,
            DiagnosticCount: diagnostics.Length + allStepDiagnostics.Length,
            WarningCount: warnings.Length + allStepWarnings.Length,
            AssumptionCount: assumptions.Length + allStepAssumptions.Length,
            Modules: moduleSet.Length == 0 ? [_rootModule] : moduleSet);

        return new CalculationTraceDocument(
            TraceId: _traceId,
            CalculationId: _calculationId,
            CalculationType: _calculationType,
            CreatedTimestampUtc: _createdTimestampUtc,
            RootModule: _rootModule,
            Steps: orderedRootSteps,
            Summary: summary,
            Assumptions: assumptions,
            Warnings: warnings,
            Diagnostics: diagnostics,
            Metadata: _metadata,
            SchemaVersion: SchemaVersion);
    }

    private void MergeStep(
        CalculationTraceStep source,
        string? parentStepId)
    {
        var mergedStepId = AddStep(
            source.ModuleKind,
            source.StepName,
            source.FormulaOrConventionLabel,
            parentStepId,
            source.StepId,
            source.DurationMilliseconds);

        foreach (var value in source.InputValues)
        {
            AddInputValue(mergedStepId, value);
        }

        foreach (var value in source.IntermediateValues)
        {
            AddIntermediateValue(mergedStepId, value);
        }

        foreach (var value in source.OutputValues)
        {
            AddOutputValue(mergedStepId, value);
        }

        foreach (var assumption in source.Assumptions)
        {
            AddAssumption(mergedStepId, assumption);
        }

        foreach (var warning in source.Warnings)
        {
            AddWarning(mergedStepId, warning);
        }

        foreach (var diagnostic in source.Diagnostics)
        {
            AddDiagnostic(mergedStepId, diagnostic);
        }

        foreach (var child in source.ChildSteps.OrderBy(item => item.Sequence))
        {
            MergeStep(child, mergedStepId);
        }
    }

    private CalculationTraceStep BuildStep(
        MutableStep step)
    {
        var children = step.ChildStepIds
            .Select(childId => _stepsById[childId])
            .OrderBy(child => child.Sequence)
            .Select(BuildStep)
            .ToArray();

        return new CalculationTraceStep(
            StepId: step.StepId,
            ModuleKind: step.ModuleKind,
            StepName: step.StepName,
            Sequence: step.Sequence,
            InputValues: step.InputValues.ToArray(),
            IntermediateValues: step.IntermediateValues.ToArray(),
            OutputValues: step.OutputValues.ToArray(),
            FormulaOrConventionLabel: step.FormulaOrConventionLabel,
            Assumptions: DistinctStrings(step.Assumptions),
            Warnings: DistinctStrings(step.Warnings),
            Diagnostics: DistinctDiagnostics(step.Diagnostics),
            ChildSteps: children,
            DurationMilliseconds: step.DurationMilliseconds);
    }

    private static IEnumerable<CalculationTraceStep> FlattenSteps(
        IEnumerable<CalculationTraceStep> steps)
    {
        foreach (var step in steps)
        {
            yield return step;
            foreach (var child in FlattenSteps(step.ChildSteps))
            {
                yield return child;
            }
        }
    }

    private void AddStepValue(
        string stepId,
        CalculationTraceValue value,
        ValueTarget target)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!_stepsById.TryGetValue(stepId, out var step))
            return;

        switch (target)
        {
            case ValueTarget.Input:
                step.InputValues.Add(value);
                break;
            case ValueTarget.Intermediate:
                step.IntermediateValues.Add(value);
                break;
            case ValueTarget.Output:
                step.OutputValues.Add(value);
                break;
        }
    }

    private string ResolveStepId(
        string? requestedStepId)
    {
        var baseId = string.IsNullOrWhiteSpace(requestedStepId)
            ? $"step-{_sequence + 1:0000}"
            : requestedStepId.Trim();

        if (!_stepsById.ContainsKey(baseId))
            return baseId;

        var suffix = 1;
        var candidate = $"{baseId}-{suffix:00}";
        while (_stepsById.ContainsKey(candidate))
        {
            suffix++;
            candidate = $"{baseId}-{suffix:00}";
        }

        return candidate;
    }

    private static string[] DistinctStrings(
        IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    private static CalculationTraceDiagnostic[] DistinctDiagnostics(
        IEnumerable<CalculationTraceDiagnostic> diagnostics) =>
        diagnostics
            .Distinct()
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ThenBy(item => item.Context, StringComparer.Ordinal)
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

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Calculation trace builder must be initialized before use.");
    }

    private bool CanCaptureInputs() => DetailLevel >= CalculationTraceDetailLevel.Standard;

    private bool CanCaptureIntermediateValues() => DetailLevel >= CalculationTraceDetailLevel.Detailed;

    private bool CanCaptureOutputs() => DetailLevel >= CalculationTraceDetailLevel.Summary;

    private bool CanCaptureAssumptionsAndWarnings() => DetailLevel >= CalculationTraceDetailLevel.Standard;

    private bool CanCaptureDiagnostics() => DetailLevel >= CalculationTraceDetailLevel.Summary;

    private enum ValueTarget
    {
        Input,
        Intermediate,
        Output
    }

    private sealed class MutableStep
    {
        public MutableStep(
            string stepId,
            CalculationTraceModuleKind moduleKind,
            string stepName,
            int sequence,
            string? formulaOrConventionLabel,
            double? durationMilliseconds)
        {
            StepId = stepId;
            ModuleKind = moduleKind;
            StepName = stepName;
            Sequence = sequence;
            FormulaOrConventionLabel = formulaOrConventionLabel;
            DurationMilliseconds = durationMilliseconds;
        }

        public string StepId { get; }

        public CalculationTraceModuleKind ModuleKind { get; }

        public string StepName { get; }

        public int Sequence { get; }

        public string? FormulaOrConventionLabel { get; }

        public double? DurationMilliseconds { get; }

        public List<CalculationTraceValue> InputValues { get; } = [];

        public List<CalculationTraceValue> IntermediateValues { get; } = [];

        public List<CalculationTraceValue> OutputValues { get; } = [];

        public List<string> Assumptions { get; } = [];

        public List<string> Warnings { get; } = [];

        public List<CalculationTraceDiagnostic> Diagnostics { get; } = [];

        public List<string> ChildStepIds { get; } = [];
    }
}
