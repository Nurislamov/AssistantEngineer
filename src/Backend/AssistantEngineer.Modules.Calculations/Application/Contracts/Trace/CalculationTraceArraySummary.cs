namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record CalculationTraceArraySummary(
    int Count,
    object? First,
    object? Last,
    double? Min = null,
    double? Max = null);
