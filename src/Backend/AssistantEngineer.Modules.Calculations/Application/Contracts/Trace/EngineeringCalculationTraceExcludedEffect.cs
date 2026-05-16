namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record EngineeringCalculationTraceExcludedEffect(
    string Effect,
    string Reason,
    string? Source = null);
