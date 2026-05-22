using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public sealed class EngineeringWorkflowReportPreviewService : IEngineeringWorkflowReportPreviewService
{
    private readonly IEngineeringReportBuilder _reportBuilder;
    private readonly IEngineeringWorkflowTracePreviewService _tracePreviewService;

    public EngineeringWorkflowReportPreviewService(
        IEngineeringReportBuilder reportBuilder,
        IEngineeringWorkflowTracePreviewService tracePreviewService)
    {
        _reportBuilder = reportBuilder;
        _tracePreviewService = tracePreviewService;
    }

    public EngineeringReportDocument BuildReportDocument(
        EngineeringWorkflowReportRequestDto request,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var detailLevel = ParseReportDetailLevel(request.DetailLevel);
        var reportKind = ParseReportKind(request.ReportKind);
        var reportFormat = ParseReportFormat(request.RequestedFormat);
        var traceDetailLevel = request.DetailLevel.Equals("Detailed", StringComparison.OrdinalIgnoreCase)
            ? CalculationTraceDetailLevel.Detailed
            : request.DetailLevel.Equals("Summary", StringComparison.OrdinalIgnoreCase)
                ? CalculationTraceDetailLevel.Summary
                : CalculationTraceDetailLevel.Standard;

        var traceDocument = request.IncludeTraceAppendix
            ? _tracePreviewService.BuildTraceDocument(request.State, traceDetailLevel, diagnostics)
            : null;

        var calculationDiagnostics = diagnostics
            .Where(diagnostic => !diagnostic.Severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
            .Select(diagnostic => new CalculationDiagnostic(
                Severity: ParseCalculationDiagnosticSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                Context: diagnostic.TargetField ?? diagnostic.SourceModule))
            .ToArray();

        var reportRequest = new EngineeringReportGenerationRequest(
            ReportKind: reportKind,
            RequestedFormat: reportFormat,
            ReportTitle: $"Engineering workflow report - {request.State.ProjectName}",
            ProjectId: request.State.ProjectId.ToString(),
            BuildingId: request.State.BuildingId?.ToString(),
            ValidationDiagnostics: calculationDiagnostics,
            CalculationTrace: traceDocument,
            DetailLevel: detailLevel,
            IncludeTraceAppendix: request.IncludeTraceAppendix,
            IncludeLimitations: request.IncludeLimitations,
            Assumptions: request.State.Assumptions,
            Warnings: diagnostics
                .Where(diagnostic => diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
                .Select(diagnostic => diagnostic.Message)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            SourceCalculationIds: request.State.CalculationTraceSummary?.CalculationId is null
                ? []
                : [request.State.CalculationTraceSummary.CalculationId],
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["workflow.mode"] = "api",
                ["workflow.step"] = request.State.CurrentStep,
                ["workflow.stage"] = "foundation"
            });

        return _reportBuilder.Build(reportRequest);
    }

    public EngineeringWorkflowReportPreviewDto BuildReportPreview(EngineeringReportDocument report)
    {
        return new EngineeringWorkflowReportPreviewDto(
            ReportKind: report.ReportKind.ToString(),
            Title: report.Title,
            Sections: report.Sections
                .OrderBy(section => section.Order)
                .Select(section => section.Title)
                .ToArray(),
            WarningsCount: report.Warnings.Count,
            DiagnosticsCount: report.Diagnostics.Count,
            ExportFormatsAvailable: ["Json", "Markdown"],
            GeneratedTimestampUtc: report.GeneratedTimestampUtc,
            Limitations: report.Sections
                .Where(section => section.SectionKind == EngineeringReportSectionKind.Limitations)
                .SelectMany(section => section.KeyValues.Select(value => value.Value?.ToString() ?? string.Empty))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }

    private static EngineeringReportDetailLevel ParseReportDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<EngineeringReportDetailLevel>(detailLevel, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportDetailLevel.Standard;
    }

    private static EngineeringReportKind ParseReportKind(string? reportKind)
    {
        if (!string.IsNullOrWhiteSpace(reportKind) &&
            Enum.TryParse<EngineeringReportKind>(reportKind, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportKind.FullEngineeringCore;
    }

    private static EngineeringReportFormat ParseReportFormat(string? format)
    {
        if (!string.IsNullOrWhiteSpace(format) &&
            Enum.TryParse<EngineeringReportFormat>(format, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportFormat.Json;
    }

    private static CalculationDiagnosticSeverity ParseCalculationDiagnosticSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationDiagnosticSeverity.Error;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationDiagnosticSeverity.Warning;
        }

        return CalculationDiagnosticSeverity.Info;
    }
}
