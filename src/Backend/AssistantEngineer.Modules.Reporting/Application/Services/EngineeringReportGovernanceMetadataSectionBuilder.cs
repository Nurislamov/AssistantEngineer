using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportGovernanceMetadataSectionBuilder
{
    private readonly EngineeringReportFormattingService _formatting;

    public EngineeringReportGovernanceMetadataSectionBuilder(
        EngineeringReportFormattingService formatting)
    {
        _formatting = formatting;
    }

    public EngineeringReportSection BuildAssumptionsSection(
        IEnumerable<string> assumptions,
        int order) =>
        new(
            SectionId: "assumptions",
            SectionKind: EngineeringReportSectionKind.Assumptions,
            Title: "Assumptions",
            Order: order,
            SummaryText: "Aggregated assumptions used across provided calculation summaries.",
            KeyValues: assumptions
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .Select((item, index) => new EngineeringReportValue($"assumption_{index + 1:000}", $"Assumption {index + 1}", item))
                .ToArray(),
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);

    public EngineeringReportSection BuildWarningsSection(
        IEnumerable<string> warnings,
        int order) =>
        new(
            SectionId: "warnings",
            SectionKind: EngineeringReportSectionKind.Warnings,
            Title: "Warnings",
            Order: order,
            SummaryText: "Aggregated warnings from calculations and trace.",
            KeyValues: warnings
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .Select((item, index) => new EngineeringReportValue($"warning_{index + 1:000}", $"Warning {index + 1}", item))
                .ToArray(),
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);

    public EngineeringReportSection BuildLimitationsSection(
        int order) =>
        new(
            SectionId: "limitations",
            SectionKind: EngineeringReportSectionKind.Limitations,
            Title: "Known Limitations",
            Order: order,
            SummaryText: "Current report generation boundaries for internal engineering usage.",
            KeyValues:
            [
                new EngineeringReportValue("limitation_001", "Limitation", "Reports summarize current internal engineering calculations only."),
                new EngineeringReportValue("limitation_002", "Limitation", "Report is not a legal compliance certificate."),
                new EngineeringReportValue("limitation_003", "Limitation", "Report is not external validation evidence."),
                new EngineeringReportValue("limitation_004", "Limitation", "Report does not prove full standard compliance."),
                new EngineeringReportValue("limitation_005", "Limitation", "PDF/HTML production rendering is not the focus of this stage."),
                new EngineeringReportValue("limitation_006", "Limitation", "Charts are placeholders unless explicitly implemented.")
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);

    public EngineeringReportSection BuildMetadataSection(
        EngineeringReportGenerationRequest request,
        DateTimeOffset generatedAt,
        string schemaVersion,
        int order)
    {
        var metadataRows = (request.Metadata ?? new Dictionary<string, string>())
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => new EngineeringReportValue(
                Key: item.Key,
                Label: item.Key,
                Value: item.Value))
            .ToList();

        metadataRows.Add(new EngineeringReportValue("generated_utc", "Generated UTC", _formatting.FormatIsoUtc(generatedAt)));
        metadataRows.Add(new EngineeringReportValue("schema_version", "Schema version", schemaVersion));

        return new EngineeringReportSection(
            SectionId: "metadata",
            SectionKind: EngineeringReportSectionKind.Metadata,
            Title: "Metadata",
            Order: order,
            SummaryText: "Deterministic metadata included in report payload.",
            KeyValues: metadataRows,
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);
    }

    public void AddAssumptionsAndWarnings(
        EngineeringReportGenerationRequest request,
        ICollection<string> assumptions,
        ICollection<string> warnings)
    {
        assumptions.AddRange(request.Assumptions ?? []);
        warnings.AddRange(request.Warnings ?? []);

        if (request.HeatingCoolingSummary is not null)
            assumptions.AddRange(request.HeatingCoolingSummary.Assumptions);

        if (request.CalculationTrace is not null)
        {
            assumptions.AddRange(request.CalculationTrace.Assumptions);
            warnings.AddRange(request.CalculationTrace.Warnings);
        }
    }

    public void AddSourceSpecificAssumptions(
        EngineeringReportGenerationRequest request,
        ICollection<string> assumptions)
    {
        if (request.SystemEnergySummary is not null)
            assumptions.AddRange(request.SystemEnergySummary.DisclosureSummary.Assumptions);
    }
}
