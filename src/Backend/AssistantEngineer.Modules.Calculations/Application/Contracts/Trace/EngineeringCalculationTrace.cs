namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record EngineeringCalculationTrace(
    string TraceId,
    string Scope,
    string SubjectType,
    int? SubjectId,
    string CalculationType,
    IReadOnlyList<EngineeringCalculationTraceSection> Sections,
    IReadOnlyList<EngineeringCalculationTraceAssumption> Assumptions,
    IReadOnlyList<EngineeringCalculationTraceExcludedEffect> ExcludedEffects,
    IReadOnlyList<EngineeringCalculationTraceDiagnosticReference> DiagnosticReferences,
    IReadOnlyDictionary<string, string>? Metadata = null);
