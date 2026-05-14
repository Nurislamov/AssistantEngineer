using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportWeatherSolarSectionBuilder
{
    public void BuildWeatherAndSolarSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                request.CalculationTrace is not null && EngineeringReportSectionSelectionPolicy.HasTraceModule(request.CalculationTrace, CalculationTraceModuleKind.Weather, CalculationTraceModuleKind.Solar),
                diagnostics,
                "weather-solar",
                CalculationTraceModuleKind.Weather,
                request.DetailLevel))
            return;

        sections.Add(new EngineeringReportSection(
            SectionId: "weather-solar",
            SectionKind: EngineeringReportSectionKind.WeatherAndSolar,
            Title: "Weather and Solar",
            Order: ++order,
            SummaryText: "Weather source and solar context from provided trace.",
            KeyValues:
            [
                new EngineeringReportValue("trace_weather_steps", "Weather steps", EngineeringReportSectionSelectionPolicy.CountTraceSteps(request.CalculationTrace, CalculationTraceModuleKind.Weather)),
                new EngineeringReportValue("trace_solar_steps", "Solar steps", EngineeringReportSectionSelectionPolicy.CountTraceSteps(request.CalculationTrace, CalculationTraceModuleKind.Solar))
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));
    }
}
