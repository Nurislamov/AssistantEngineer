namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

public sealed record EngineeringCalculationModeDiagnostic(
    string Code,
    string Message,
    string? Context = null);
