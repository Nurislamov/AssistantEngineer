using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportDiagnosticsSectionBuilder
{
    private readonly IEngineeringReportDiagnosticAggregator _diagnosticAggregator;

    public EngineeringReportDiagnosticsSectionBuilder(
        IEngineeringReportDiagnosticAggregator diagnosticAggregator)
    {
        _diagnosticAggregator = diagnosticAggregator;
    }

    public void AddDiagnosticsFromRequest(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportDiagnostic> diagnostics)
    {
        if (request.ValidationDiagnostics is not null)
        {
            diagnostics.AddRange(request.ValidationDiagnostics.Select(item =>
                _diagnosticAggregator.FromCalculationDiagnostic(item, CalculationTraceModuleKind.Validation, "ValidationDiagnostics")));
        }

        if (request.StandardDiagnostics is not null)
        {
            diagnostics.AddRange(request.StandardDiagnostics.Select(item =>
                _diagnosticAggregator.FromStandardDiagnostic(item, CalculationTraceModuleKind.Validation, "StandardDiagnostics")));
        }

        if (request.HeatingCoolingSummary is not null)
        {
            diagnostics.AddRange(request.HeatingCoolingSummary.Diagnostics.Select(item =>
                _diagnosticAggregator.FromCalculationDiagnostic(item, CalculationTraceModuleKind.Iso52016, "HeatingCoolingSummary")));
        }

        if (request.DomesticHotWaterSummary is not null)
        {
            diagnostics.AddRange(request.DomesticHotWaterSummary.Diagnostics.Select(item =>
                _diagnosticAggregator.FromStandardDiagnostic(item, CalculationTraceModuleKind.DomesticHotWater, "DomesticHotWaterSummary")));
        }

        if (request.NaturalVentilationSummary is not null)
        {
            diagnostics.AddRange(request.NaturalVentilationSummary.Diagnostics.Select(item =>
                _diagnosticAggregator.FromStandardDiagnostic(item, CalculationTraceModuleKind.Ventilation, "NaturalVentilationSummary")));
        }

        if (request.GroundSummary is not null)
        {
            diagnostics.AddRange(request.GroundSummary.Diagnostics.Select(item =>
                _diagnosticAggregator.FromStandardDiagnostic(item, CalculationTraceModuleKind.Ground, "GroundSummary")));
        }

        if (request.SystemEnergySummary is not null)
        {
            diagnostics.AddRange(request.SystemEnergySummary.Diagnostics.Select(item =>
                _diagnosticAggregator.FromStandardDiagnostic(item, CalculationTraceModuleKind.SystemEnergy, "SystemEnergySummary")));
        }
    }

    public void BuildValidationSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                (request.ValidationDiagnostics?.Count ?? 0) > 0 || (request.StandardDiagnostics?.Count ?? 0) > 0,
                diagnostics,
                "validation",
                CalculationTraceModuleKind.Validation,
                request.DetailLevel))
            return;

        var validationDiagnostics = new List<EngineeringReportDiagnostic>();
        if (request.ValidationDiagnostics is not null)
        {
            validationDiagnostics.AddRange(request.ValidationDiagnostics.Select(item =>
                _diagnosticAggregator.FromCalculationDiagnostic(item, CalculationTraceModuleKind.Validation, "Validation")));
        }

        if (request.StandardDiagnostics is not null)
        {
            validationDiagnostics.AddRange(request.StandardDiagnostics.Select(item =>
                _diagnosticAggregator.FromStandardDiagnostic(item, CalculationTraceModuleKind.Validation, "Validation")));
        }

        var aggregated = _diagnosticAggregator.Aggregate(validationDiagnostics);
        diagnostics.AddRange(aggregated);

        sections.Add(new EngineeringReportSection(
            SectionId: "validation",
            SectionKind: EngineeringReportSectionKind.ValidationDiagnostics,
            Title: "Validation Diagnostics",
            Order: ++order,
            SummaryText: "Merged validation diagnostics from provided calculation summaries.",
            KeyValues:
            [
                new EngineeringReportValue("validation_diagnostic_count", "Diagnostic count", aggregated.Count)
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: aggregated,
            Assumptions: [],
            ChildSections: []));
    }

    public void BuildTraceAppendixSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        int summaryTraceStepLimit,
        int standardTraceStepLimit,
        ref int order)
    {
        if (!request.IncludeTraceAppendix)
            return;

        if (request.CalculationTrace is null)
        {
            diagnostics.Add(new EngineeringReportDiagnostic(
                EngineeringReportDiagnosticSeverity.Info,
                "AE-REPORT-TRACE-MISSING",
                "Calculation trace was not provided; trace appendix is omitted.",
                CalculationTraceModuleKind.Reporting,
                "TraceAppendix",
                "EngineeringReportBuilder"));
            return;
        }

        var trace = request.CalculationTrace;
        assumptions.AddRange(trace.Assumptions);
        warnings.AddRange(trace.Warnings);
        diagnostics.AddRange(trace.Diagnostics.Select(_diagnosticAggregator.FromTraceDiagnostic));

        var limit = request.DetailLevel == EngineeringReportDetailLevel.Detailed
            ? trace.Steps.Count
            : request.DetailLevel == EngineeringReportDetailLevel.Standard
                ? standardTraceStepLimit
                : summaryTraceStepLimit;

        var limitedSteps = EngineeringReportSectionSelectionPolicy.LimitTraceSteps(trace.Steps, limit);
        var rows = limitedSteps
            .Select(step => (IReadOnlyList<string>)
            [
                step.StepId,
                step.StepName,
                step.ModuleKind.ToString(),
                step.InputValues.Count.ToString(),
                step.IntermediateValues.Count.ToString(),
                step.OutputValues.Count.ToString(),
                step.Diagnostics.Count.ToString()
            ])
            .ToArray();

        sections.Add(new EngineeringReportSection(
            SectionId: "trace-appendix",
            SectionKind: EngineeringReportSectionKind.CalculationTraceAppendix,
            Title: "Calculation Trace Appendix",
            Order: ++order,
            SummaryText: request.DetailLevel == EngineeringReportDetailLevel.Detailed
                ? "Detailed trace appendix with step-level inventory."
                : "Compact trace appendix summary for report readability.",
            KeyValues:
            [
                new EngineeringReportValue("trace_id", "Trace id", trace.TraceId),
                new EngineeringReportValue("trace_calculation_type", "Trace calculation type", trace.CalculationType),
                new EngineeringReportValue("trace_step_count", "Trace step count", trace.Summary.StepCount),
                new EngineeringReportValue("trace_diagnostic_count", "Trace diagnostic count", trace.Summary.DiagnosticCount),
                new EngineeringReportValue("trace_modules", "Trace modules", string.Join(", ", trace.Summary.Modules.OrderBy(item => item).Select(item => item.ToString())))
            ],
            Tables:
            [
                new EngineeringReportTable(
                    TableId: "trace-step-summary",
                    Title: "Trace step summary",
                    Columns: ["StepId", "StepName", "Module", "Inputs", "Intermediate", "Outputs", "Diagnostics"],
                    Rows: rows,
                    Units: new Dictionary<string, string>(),
                    Notes: request.DetailLevel == EngineeringReportDetailLevel.Detailed
                        ? []
                        : ["Trace steps are compacted for non-detailed report detail levels."])
            ],
            ChartPlaceholders: [],
            Diagnostics: _diagnosticAggregator.Aggregate(trace.Diagnostics.Select(_diagnosticAggregator.FromTraceDiagnostic)),
            Assumptions: trace.Assumptions
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray(),
            ChildSections: []));
    }

    public EngineeringReportSection BuildDiagnosticsSection(
        IEnumerable<EngineeringReportDiagnostic> diagnostics,
        int order)
    {
        var aggregated = _diagnosticAggregator.Aggregate(diagnostics);
        return new EngineeringReportSection(
            SectionId: "diagnostics",
            SectionKind: EngineeringReportSectionKind.ValidationDiagnostics,
            Title: "Diagnostics",
            Order: order,
            SummaryText: "Compact merged diagnostics grouped deterministically by severity and module.",
            KeyValues:
            [
                new EngineeringReportValue("diagnostic_count", "Diagnostic count", aggregated.Count),
                new EngineeringReportValue("error_count", "Error count", aggregated.Count(item => item.Severity == EngineeringReportDiagnosticSeverity.Error)),
                new EngineeringReportValue("warning_count", "Warning count", aggregated.Count(item => item.Severity == EngineeringReportDiagnosticSeverity.Warning))
            ],
            Tables:
            [
                new EngineeringReportTable(
                    TableId: "diagnostics-table",
                    Title: "Diagnostics",
                    Columns: ["Severity", "Module", "Code", "Message", "SuggestedCorrection"],
                    Rows: aggregated.Select(item => (IReadOnlyList<string>)
                    [
                        item.Severity.ToString(),
                        item.Module.ToString(),
                        item.Code,
                        item.Message,
                        item.SuggestedCorrection ?? string.Empty
                    ]).ToArray(),
                    Units: new Dictionary<string, string>(),
                    Notes: [])
            ],
            ChartPlaceholders: [],
            Diagnostics: aggregated,
            Assumptions: [],
            ChildSections: []);
    }
}
