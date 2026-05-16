namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record EngineeringCalculationTraceSection(
    string SectionId,
    string Title,
    string Category,
    IReadOnlyList<EngineeringCalculationTraceLine> Lines,
    IReadOnlyDictionary<string, string>? Metadata = null);
