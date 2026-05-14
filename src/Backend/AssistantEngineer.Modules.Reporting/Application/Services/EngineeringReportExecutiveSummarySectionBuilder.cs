using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportExecutiveSummarySectionBuilder
{
    public EngineeringReportSection BuildExecutiveSummarySection(
        EngineeringReportGenerationRequest request,
        int order)
    {
        var availableModules = GetAvailableModules(request);
        return new EngineeringReportSection(
            SectionId: "executive-summary",
            SectionKind: EngineeringReportSectionKind.ExecutiveSummary,
            Title: "Executive Summary",
            Order: order,
            SummaryText: "Internal engineering implementation summary for deterministic report generation.",
            KeyValues:
            [
                new EngineeringReportValue("report_kind", "Report kind", request.ReportKind.ToString()),
                new EngineeringReportValue("requested_format", "Requested format", request.RequestedFormat.ToString()),
                new EngineeringReportValue("detail_level", "Detail level", request.DetailLevel.ToString()),
                new EngineeringReportValue("available_modules", "Available modules", string.Join(", ", availableModules)),
                new EngineeringReportValue("module_count", "Available module count", availableModules.Count)
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);
    }

    private static IReadOnlyList<string> GetAvailableModules(
        EngineeringReportGenerationRequest request)
    {
        var modules = new SortedSet<string>(StringComparer.Ordinal)
        {
            "Reporting"
        };

        if (request.HeatingCoolingSummary is not null)
            modules.Add("Iso52016");

        if (request.MultiZoneSummary is not null)
            modules.Add("MultiZone");

        if (request.NaturalVentilationSummary is not null)
            modules.Add("Ventilation");

        if (request.GroundSummary is not null)
            modules.Add("Ground");

        if (request.DomesticHotWaterSummary is not null)
            modules.Add("DomesticHotWater");

        if (request.SystemEnergySummary is not null)
            modules.Add("SystemEnergy");

        if (request.CalculationTrace is not null)
        {
            foreach (var module in request.CalculationTrace.Summary.Modules.Select(item => item.ToString()))
                modules.Add(module);
        }

        return modules.ToArray();
    }
}
