namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;

public sealed record SolarGainDiagnostic(
    SolarGainDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Context = null);
