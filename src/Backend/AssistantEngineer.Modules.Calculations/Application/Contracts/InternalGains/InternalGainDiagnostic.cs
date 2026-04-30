namespace AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;

public sealed record InternalGainDiagnostic(
    InternalGainDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Context = null);