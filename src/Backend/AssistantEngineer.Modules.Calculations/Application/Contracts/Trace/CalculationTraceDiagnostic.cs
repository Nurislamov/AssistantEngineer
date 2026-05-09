namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record CalculationTraceDiagnostic(
    CalculationTraceSeverity Severity,
    string Code,
    string Message,
    CalculationTraceModuleKind ModuleKind,
    string? Context = null,
    string? Source = null);
