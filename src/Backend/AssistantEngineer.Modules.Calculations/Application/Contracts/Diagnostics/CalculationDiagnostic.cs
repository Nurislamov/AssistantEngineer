namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

public sealed record CalculationDiagnostic(
    CalculationDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Context = null);
