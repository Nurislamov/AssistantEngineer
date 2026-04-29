namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record VentilationLoadDiagnostic(
    VentilationLoadDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Context = null);
