namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record EngineeringCalculationTraceLine(
    string LineId,
    string Label,
    string? Formula,
    IReadOnlyDictionary<string, double>? Inputs,
    string? Unit,
    double? Value,
    string? Explanation,
    string? Source,
    IReadOnlyDictionary<string, string>? Metadata = null);
