namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record CalculationTraceUnit(
    string Symbol,
    string? QuantityKind = null,
    string? DisplayName = null);
