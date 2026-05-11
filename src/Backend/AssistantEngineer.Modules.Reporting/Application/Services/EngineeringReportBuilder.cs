using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportBuilder : IEngineeringReportBuilder
{
    private const string SchemaVersion = "1.0";
    private const int StandardTraceStepLimit = 20;
    private const int SummaryTraceStepLimit = 8;

    private readonly TimeProvider _timeProvider;
    private readonly IEngineeringReportDiagnosticAggregator _diagnosticAggregator;
    private readonly EngineeringReportSectionBuilder _sectionBuilder;
    private readonly EngineeringReportDiagnosticsSectionBuilder _diagnosticsSectionBuilder;

    public EngineeringReportBuilder(
        TimeProvider timeProvider,
        IEngineeringReportDiagnosticAggregator diagnosticAggregator)
    {
        _timeProvider = timeProvider;
        _diagnosticAggregator = diagnosticAggregator;

        var formatting = new EngineeringReportFormattingService();
        _sectionBuilder = new EngineeringReportSectionBuilder(_diagnosticAggregator, formatting);
        _diagnosticsSectionBuilder = new EngineeringReportDiagnosticsSectionBuilder(_diagnosticAggregator);
    }

    public EngineeringReportDocument Build(
        EngineeringReportGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var generatedAt = request.DeterministicTimestampUtc ?? _timeProvider.GetUtcNow();
        var reportId = $"report-{generatedAt:yyyyMMddHHmmssfff}-{request.ReportKind}";
        var reportTitle = string.IsNullOrWhiteSpace(request.ReportTitle)
            ? $"{request.ReportKind} Engineering Report"
            : request.ReportTitle.Trim();

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<EngineeringReportDiagnostic>();
        var summaries = new List<EngineeringReportValue>();
        var sections = new List<EngineeringReportSection>();
        var order = 0;

        _sectionBuilder.AddAssumptionsAndWarnings(request, assumptions, warnings);
        _diagnosticsSectionBuilder.AddDiagnosticsFromRequest(request, diagnostics);
        _sectionBuilder.AddSourceSpecificAssumptions(request, assumptions);

        sections.Add(_sectionBuilder.BuildExecutiveSummarySection(request, ++order));
        sections.Add(_sectionBuilder.BuildInputSummarySection(request, ++order));

        _sectionBuilder.BuildWeatherAndSolarSection(request, sections, diagnostics, ref order);
        _sectionBuilder.BuildThermalZonesSection(request, sections, diagnostics, ref order);
        _sectionBuilder.BuildHeatingCoolingSection(request, sections, assumptions, diagnostics, summaries, ref order);
        _sectionBuilder.BuildNaturalVentilationSection(request, sections, diagnostics, summaries, ref order);
        _sectionBuilder.BuildGroundSection(request, sections, diagnostics, summaries, ref order);
        _sectionBuilder.BuildDomesticHotWaterSection(request, sections, diagnostics, summaries, ref order);
        _sectionBuilder.BuildSystemEnergySections(request, sections, assumptions, diagnostics, summaries, ref order);

        _diagnosticsSectionBuilder.BuildValidationSection(request, sections, diagnostics, ref order);
        _diagnosticsSectionBuilder.BuildTraceAppendixSection(
            request,
            sections,
            assumptions,
            warnings,
            diagnostics,
            summaryTraceStepLimit: SummaryTraceStepLimit,
            standardTraceStepLimit: StandardTraceStepLimit,
            ref order);

        sections.Add(_sectionBuilder.BuildAssumptionsSection(assumptions, ++order));
        sections.Add(_sectionBuilder.BuildWarningsSection(warnings, ++order));
        sections.Add(_diagnosticsSectionBuilder.BuildDiagnosticsSection(diagnostics, ++order));

        if (request.IncludeLimitations)
            sections.Add(_sectionBuilder.BuildLimitationsSection(++order));

        sections.Add(_sectionBuilder.BuildMetadataSection(request, generatedAt, SchemaVersion, ++order));

        var orderedSections = sections
            .OrderBy(item => item.Order)
            .ThenBy(item => item.SectionId, StringComparer.Ordinal)
            .ToArray();

        var documentDiagnostics = _diagnosticAggregator.Aggregate(diagnostics);
        var documentAssumptions = assumptions
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        var documentWarnings = warnings
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        var documentMetadata = (request.Metadata ?? new Dictionary<string, string>())
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key.Trim(), item => item.Value?.Trim() ?? string.Empty, StringComparer.Ordinal);

        return new EngineeringReportDocument(
            ReportId: reportId,
            ReportKind: request.ReportKind,
            Title: reportTitle,
            ProjectId: request.ProjectId,
            BuildingId: request.BuildingId,
            GeneratedTimestampUtc: generatedAt,
            SchemaVersion: SchemaVersion,
            Format: request.RequestedFormat,
            Sections: orderedSections,
            Summaries: summaries
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToArray(),
            Warnings: documentWarnings,
            Diagnostics: documentDiagnostics,
            Assumptions: documentAssumptions,
            SourceCalculationIds: (request.SourceCalculationIds ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray(),
            TraceAppendix: request.IncludeTraceAppendix ? request.CalculationTrace : null,
            Metadata: documentMetadata);
    }
}
