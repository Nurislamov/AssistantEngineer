using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportSectionBuilder
{
    private readonly EngineeringReportExecutiveSummarySectionBuilder _executiveSummary;
    private readonly EngineeringReportInputSummarySectionBuilder _inputSummary;
    private readonly EngineeringReportWeatherSolarSectionBuilder _weatherSolar;
    private readonly EngineeringReportLoadResultsSectionBuilder _loadResults;
    private readonly EngineeringReportSystemEnergySectionBuilder _systemEnergy;
    private readonly EngineeringReportGovernanceMetadataSectionBuilder _governanceMetadata;

    public EngineeringReportSectionBuilder(
        IEngineeringReportDiagnosticAggregator diagnosticAggregator,
        EngineeringReportFormattingService formatting)
    {
        _executiveSummary = new EngineeringReportExecutiveSummarySectionBuilder();
        _inputSummary = new EngineeringReportInputSummarySectionBuilder();
        _weatherSolar = new EngineeringReportWeatherSolarSectionBuilder();
        _loadResults = new EngineeringReportLoadResultsSectionBuilder(formatting);
        _systemEnergy = new EngineeringReportSystemEnergySectionBuilder(diagnosticAggregator, formatting);
        _governanceMetadata = new EngineeringReportGovernanceMetadataSectionBuilder(formatting);
    }

    public EngineeringReportSection BuildExecutiveSummarySection(
        EngineeringReportGenerationRequest request,
        int order) =>
        _executiveSummary.BuildExecutiveSummarySection(request, order);

    public EngineeringReportSection BuildInputSummarySection(
        EngineeringReportGenerationRequest request,
        int order) =>
        _inputSummary.BuildInputSummarySection(request, order);

    public void BuildWeatherAndSolarSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ref int order) =>
        _weatherSolar.BuildWeatherAndSolarSection(request, sections, diagnostics, ref order);

    public void BuildThermalZonesSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ref int order) =>
        _loadResults.BuildThermalZonesSection(request, sections, diagnostics, ref order);

    public void BuildHeatingCoolingSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<string> assumptions,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order) =>
        _loadResults.BuildHeatingCoolingSection(request, sections, assumptions, diagnostics, summaries, ref order);

    public void BuildNaturalVentilationSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order) =>
        _loadResults.BuildNaturalVentilationSection(request, sections, diagnostics, summaries, ref order);

    public void BuildGroundSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order) =>
        _loadResults.BuildGroundSection(request, sections, diagnostics, summaries, ref order);

    public void BuildDomesticHotWaterSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order) =>
        _loadResults.BuildDomesticHotWaterSection(request, sections, diagnostics, summaries, ref order);

    public void BuildSystemEnergySections(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<string> assumptions,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order) =>
        _systemEnergy.BuildSystemEnergySections(request, sections, assumptions, diagnostics, summaries, ref order);

    public EngineeringReportSection BuildAssumptionsSection(
        IEnumerable<string> assumptions,
        int order) =>
        _governanceMetadata.BuildAssumptionsSection(assumptions, order);

    public EngineeringReportSection BuildWarningsSection(
        IEnumerable<string> warnings,
        int order) =>
        _governanceMetadata.BuildWarningsSection(warnings, order);

    public EngineeringReportSection BuildLimitationsSection(
        int order) =>
        _governanceMetadata.BuildLimitationsSection(order);

    public EngineeringReportSection BuildMetadataSection(
        EngineeringReportGenerationRequest request,
        DateTimeOffset generatedAt,
        string schemaVersion,
        int order) =>
        _governanceMetadata.BuildMetadataSection(request, generatedAt, schemaVersion, order);

    public void AddAssumptionsAndWarnings(
        EngineeringReportGenerationRequest request,
        ICollection<string> assumptions,
        ICollection<string> warnings) =>
        _governanceMetadata.AddAssumptionsAndWarnings(request, assumptions, warnings);

    public void AddSourceSpecificAssumptions(
        EngineeringReportGenerationRequest request,
        ICollection<string> assumptions) =>
        _governanceMetadata.AddSourceSpecificAssumptions(request, assumptions);
}
