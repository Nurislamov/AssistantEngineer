namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record CalculationTraceSummary(
    int StepCount,
    int DiagnosticCount,
    int WarningCount,
    int AssumptionCount,
    IReadOnlyList<CalculationTraceModuleKind> Modules);
