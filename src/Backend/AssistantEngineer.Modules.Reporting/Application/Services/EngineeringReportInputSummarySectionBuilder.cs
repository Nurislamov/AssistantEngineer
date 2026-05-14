using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportInputSummarySectionBuilder
{
    public EngineeringReportSection BuildInputSummarySection(
        EngineeringReportGenerationRequest request,
        int order) =>
        new(
            SectionId: "input-summary",
            SectionKind: EngineeringReportSectionKind.InputSummary,
            Title: "Input Summary",
            Order: order,
            SummaryText: "Provided calculation summaries and identifiers.",
            KeyValues:
            [
                new EngineeringReportValue("project_id", "Project id", request.ProjectId ?? "n/a"),
                new EngineeringReportValue("building_id", "Building id", request.BuildingId ?? "n/a"),
                new EngineeringReportValue("source_calculation_count", "Source calculation count", request.SourceCalculationIds?.Count ?? 0),
                new EngineeringReportValue("trace_provided", "Calculation trace provided", request.CalculationTrace is not null)
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);
}
