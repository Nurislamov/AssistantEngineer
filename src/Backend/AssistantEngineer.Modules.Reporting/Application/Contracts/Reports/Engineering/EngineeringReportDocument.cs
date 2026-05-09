using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

public sealed record EngineeringReportDocument(
    string ReportId,
    EngineeringReportKind ReportKind,
    string Title,
    string? ProjectId,
    string? BuildingId,
    DateTimeOffset GeneratedTimestampUtc,
    string SchemaVersion,
    EngineeringReportFormat Format,
    IReadOnlyList<EngineeringReportSection> Sections,
    IReadOnlyList<EngineeringReportValue> Summaries,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EngineeringReportDiagnostic> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> SourceCalculationIds,
    CalculationTraceDocument? TraceAppendix,
    IReadOnlyDictionary<string, string> Metadata);

