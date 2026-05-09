namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

public sealed record EngineeringReportSection(
    string SectionId,
    EngineeringReportSectionKind SectionKind,
    string Title,
    int Order,
    string? SummaryText,
    IReadOnlyList<EngineeringReportValue> KeyValues,
    IReadOnlyList<EngineeringReportTable> Tables,
    IReadOnlyList<string> ChartPlaceholders,
    IReadOnlyList<EngineeringReportDiagnostic> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<EngineeringReportSection> ChildSections);

