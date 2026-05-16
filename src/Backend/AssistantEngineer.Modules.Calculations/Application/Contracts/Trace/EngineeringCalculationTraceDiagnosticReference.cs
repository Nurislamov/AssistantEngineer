namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record EngineeringCalculationTraceDiagnosticReference(
    string Code,
    string Severity,
    string Category,
    string Message);
