namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record CalculationTraceStep(
    string StepId,
    CalculationTraceModuleKind ModuleKind,
    string StepName,
    int Sequence,
    IReadOnlyList<CalculationTraceValue> InputValues,
    IReadOnlyList<CalculationTraceValue> IntermediateValues,
    IReadOnlyList<CalculationTraceValue> OutputValues,
    string? FormulaOrConventionLabel,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<CalculationTraceDiagnostic> Diagnostics,
    IReadOnlyList<CalculationTraceStep> ChildSteps,
    double? DurationMilliseconds = null);
