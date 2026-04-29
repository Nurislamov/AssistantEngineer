namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;

public sealed record TransmissionDiagnostic(
    TransmissionDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Context = null);
