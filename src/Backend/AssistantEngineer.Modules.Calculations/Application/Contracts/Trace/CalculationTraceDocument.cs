namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record CalculationTraceDocument(
    string TraceId,
    string? CalculationId,
    string CalculationType,
    DateTimeOffset? CreatedTimestampUtc,
    CalculationTraceModuleKind RootModule,
    IReadOnlyList<CalculationTraceStep> Steps,
    CalculationTraceSummary Summary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<CalculationTraceDiagnostic> Diagnostics,
    IReadOnlyDictionary<string, string> Metadata,
    string SchemaVersion);
