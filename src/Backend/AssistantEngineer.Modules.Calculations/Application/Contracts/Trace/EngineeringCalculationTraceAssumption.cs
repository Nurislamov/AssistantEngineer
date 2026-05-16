namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record EngineeringCalculationTraceAssumption(
    string AssumptionId,
    string Name,
    string Value,
    string Unit,
    string Status,
    string Source,
    string? RegistryReference = null);
