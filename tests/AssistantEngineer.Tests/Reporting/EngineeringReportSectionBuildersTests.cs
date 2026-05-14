using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;
using AssistantEngineer.Modules.Reporting.Application.Services;

namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportSectionBuildersTests
{
    private readonly EngineeringReportDiagnosticAggregator _aggregator = new();
    private readonly EngineeringReportSectionBuilder _sectionBuilder;
    private readonly EngineeringReportDiagnosticsSectionBuilder _diagnosticsBuilder;

    public EngineeringReportSectionBuildersTests()
    {
        var formatting = new EngineeringReportFormattingService();
        _sectionBuilder = new EngineeringReportSectionBuilder(_aggregator, formatting);
        _diagnosticsBuilder = new EngineeringReportDiagnosticsSectionBuilder(_aggregator);
    }

    [Fact]
    public void DiagnosticsSectionBuilder_HandlesEmptyDiagnostics()
    {
        var section = _diagnosticsBuilder.BuildDiagnosticsSection([], order: 1);

        Assert.Equal(EngineeringReportSectionKind.ValidationDiagnostics, section.SectionKind);
        Assert.Contains(section.KeyValues, item => item.Key == "diagnostic_count" && Equals(item.Value, 0));
        Assert.Single(section.Tables);
        Assert.Empty(section.Tables[0].Rows);
    }

    [Fact]
    public void DiagnosticsSectionBuilder_BuildsValidationSectionWithWarningsAndErrors()
    {
        var request = EngineeringReportTestData.CreateMinimalRequest() with
        {
            ReportKind = EngineeringReportKind.Validation,
            ValidationDiagnostics =
            [
                new CalculationDiagnostic(CalculationDiagnosticSeverity.Warning, "WARN_001", "warning message"),
                new CalculationDiagnostic(CalculationDiagnosticSeverity.Error, "ERR_001", "error message")
            ]
        };

        var sections = new List<EngineeringReportSection>();
        var diagnostics = new List<EngineeringReportDiagnostic>();
        var order = 0;

        _diagnosticsBuilder.BuildValidationSection(request, sections, diagnostics, ref order);

        Assert.Single(sections);
        Assert.Equal("validation", sections[0].SectionId);
        Assert.True(diagnostics.Count >= 2);
        Assert.Contains(diagnostics, item => item.Code == "WARN_001");
        Assert.Contains(diagnostics, item => item.Code == "ERR_001");
    }

    [Fact]
    public void ModuleSectionBuilder_MissingHeatingCoolingDataAddsMissingDataDiagnostic()
    {
        var request = EngineeringReportTestData.CreateFullRequest(includeTrace: false) with
        {
            HeatingCoolingSummary = null
        };

        var sections = new List<EngineeringReportSection>();
        var assumptions = new List<string>();
        var diagnostics = new List<EngineeringReportDiagnostic>();
        var summaries = new List<EngineeringReportValue>();
        var order = 0;

        _sectionBuilder.BuildHeatingCoolingSection(request, sections, assumptions, diagnostics, summaries, ref order);

        Assert.Empty(sections);
        Assert.Contains(diagnostics, item => item.Code == "AE-REPORT-SECTION-DATA-MISSING");
    }

    [Fact]
    public void InputSummaryBuilder_HandlesMinimalInput()
    {
        var request = EngineeringReportTestData.CreateMinimalRequest();

        var section = _sectionBuilder.BuildInputSummarySection(request, order: 2);

        Assert.Equal("input-summary", section.SectionId);
        Assert.Contains(section.KeyValues, item => item.Key == "project_id");
        Assert.Contains(section.KeyValues, item => item.Key == "building_id");
    }

    [Fact]
    public void TraceAppendixBuilder_CompactsStepsInSummaryMode()
    {
        var request = EngineeringReportTestData.CreateFullRequest(includeTrace: true) with
        {
            DetailLevel = EngineeringReportDetailLevel.Summary
        };

        var sections = new List<EngineeringReportSection>();
        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<EngineeringReportDiagnostic>();
        var order = 0;

        _diagnosticsBuilder.BuildTraceAppendixSection(
            request,
            sections,
            assumptions,
            warnings,
            diagnostics,
            summaryTraceStepLimit: 1,
            standardTraceStepLimit: 5,
            ref order);

        Assert.Single(sections);
        Assert.Equal(EngineeringReportSectionKind.CalculationTraceAppendix, sections[0].SectionKind);
        Assert.Single(sections[0].Tables[0].Rows);
    }

    [Fact]
    public void MetadataSectionBuilder_PreservesMetadataKeysAndStandardMetadataRows()
    {
        var request = EngineeringReportTestData.CreateMinimalRequest() with
        {
            Metadata = new Dictionary<string, string>
            {
                ["z_key"] = "z",
                ["a_key"] = "a"
            }
        };

        var section = _sectionBuilder.BuildMetadataSection(
            request,
            generatedAt: EngineeringReportTestData.FixedTimestamp,
            schemaVersion: "1.0",
            order: 99);

        var keys = section.KeyValues.Select(item => item.Key).ToArray();
        Assert.Equal("a_key", keys[0]);
        Assert.Equal("z_key", keys[1]);
        Assert.Contains(section.KeyValues, item => item.Key == "generated_utc");
        Assert.Contains(section.KeyValues, item => item.Key == "schema_version");
    }

    [Fact]
    public void SystemEnergySectionBuilder_PreservesSectionOrderingAndDiagnostics()
    {
        var request = EngineeringReportTestData.CreateSystemEnergyRequest();
        var sections = new List<EngineeringReportSection>();
        var assumptions = new List<string>();
        var diagnostics = new List<EngineeringReportDiagnostic>();
        var summaries = new List<EngineeringReportValue>();
        var order = 10;

        _sectionBuilder.BuildSystemEnergySections(
            request,
            sections,
            assumptions,
            diagnostics,
            summaries,
            ref order);

        Assert.Equal(3, sections.Count);
        Assert.Equal(["system-energy", "final-energy", "primary-energy-carbon"], sections.Select(item => item.SectionId).ToArray());
        Assert.Equal([11, 12, 13], sections.Select(item => item.Order).ToArray());
        Assert.Contains(diagnostics, item => item.Code == "AE-SYS-FACTOR-FALLBACK");
    }

    [Fact]
    public void ModuleSectionBuilder_MissingSystemEnergyDataAddsMissingDataDiagnostic()
    {
        var request = EngineeringReportTestData.CreateSystemEnergyRequest() with
        {
            SystemEnergySummary = null
        };

        var sections = new List<EngineeringReportSection>();
        var assumptions = new List<string>();
        var diagnostics = new List<EngineeringReportDiagnostic>();
        var summaries = new List<EngineeringReportValue>();
        var order = 0;

        _sectionBuilder.BuildSystemEnergySections(
            request,
            sections,
            assumptions,
            diagnostics,
            summaries,
            ref order);

        Assert.Empty(sections);
        Assert.Contains(diagnostics, item => item.Code == "AE-REPORT-SECTION-DATA-MISSING");
    }
}
