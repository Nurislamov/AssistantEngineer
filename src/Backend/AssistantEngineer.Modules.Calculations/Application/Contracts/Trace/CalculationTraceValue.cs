namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record CalculationTraceValue(
    string Key,
    string Label,
    object? Value,
    CalculationTraceUnit? Unit,
    CalculationTraceValueKind ValueKind,
    string? Source = null,
    string? DisplayFormat = null,
    IReadOnlyList<string>? Tags = null);
