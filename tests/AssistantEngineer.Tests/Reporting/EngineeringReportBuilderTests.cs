using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;
using AssistantEngineer.Modules.Reporting.Application.Services;

namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportBuilderTests
{
    private readonly EngineeringReportBuilder _builder = new(
        new FixedTimeProvider(EngineeringReportTestData.FixedTimestamp),
        new EngineeringReportDiagnosticAggregator());

    [Fact]
    public void BuildsMinimalReport()
    {
        var fixture = EngineeringReportFixtureLoader.Load("minimal-calculation-summary-report.json");
        var report = _builder.Build(EngineeringReportTestData.CreateMinimalRequest());

        Assert.Equal(EngineeringReportKind.CalculationSummary, report.ReportKind);
        Assert.Equal("1.0", report.SchemaVersion);
        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.ExecutiveSummary);
        Assert.Equal(fixture.ExpectedSections, report.Sections.Select(item => item.SectionKind.ToString()).ToArray());
    }

    [Fact]
    public void BuildsHeatingCoolingReport()
    {
        var fixture = EngineeringReportFixtureLoader.Load("heating-cooling-load-report.json");
        var report = _builder.Build(EngineeringReportTestData.CreateHeatingCoolingRequest());

        Assert.Equal(EngineeringReportKind.HeatingCoolingLoad, report.ReportKind);
        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.HeatingCoolingLoads);
        Assert.Equal(fixture.ExpectedSections, report.Sections.Select(item => item.SectionKind.ToString()).ToArray());
    }

    [Fact]
    public void BuildsDhwReport()
    {
        var fixture = EngineeringReportFixtureLoader.Load("dhw-report.json");
        var report = _builder.Build(EngineeringReportTestData.CreateDhwRequest());

        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.DomesticHotWater);
        Assert.Contains(report.Summaries, item => item.Key == "dhw_system_load_kwh");
        Assert.Equal(fixture.ExpectedSections, report.Sections.Select(item => item.SectionKind.ToString()).ToArray());
    }

    [Fact]
    public void BuildsSystemEnergyReport()
    {
        var fixture = EngineeringReportFixtureLoader.Load("system-energy-report.json");
        var report = _builder.Build(EngineeringReportTestData.CreateSystemEnergyRequest());

        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.SystemEnergy);
        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.FinalEnergy);
        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.PrimaryEnergyAndCarbon);
        Assert.Equal(fixture.ExpectedSections, report.Sections.Select(item => item.SectionKind.ToString()).ToArray());
    }

    [Fact]
    public void BuildsFullEngineeringCoreReport()
    {
        var fixture = EngineeringReportFixtureLoader.Load("full-engineering-core-report.json");
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));

        Assert.Equal(EngineeringReportKind.FullEngineeringCore, report.ReportKind);
        Assert.Equal(fixture.ExpectedSections, report.Sections.Select(item => item.SectionKind.ToString()).ToArray());
        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.CalculationTraceAppendix);
    }

    [Fact]
    public void SectionOrderingIsDeterministic()
    {
        var left = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var right = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));

        Assert.Equal(
            left.Sections.Select(item => $"{item.Order}:{item.SectionKind}"),
            right.Sections.Select(item => $"{item.Order}:{item.SectionKind}"));
    }

    [Fact]
    public void MissingModulesProduceDiagnosticsNotCrash()
    {
        var fixture = EngineeringReportFixtureLoader.Load("partial-report-missing-modules.json");
        var request = EngineeringReportTestData.CreateFullRequest(includeTrace: false) with
        {
            HeatingCoolingSummary = null,
            MultiZoneSummary = null,
            NaturalVentilationSummary = null,
            GroundSummary = null
        };

        var report = _builder.Build(request);

        Assert.Contains(report.Diagnostics, item => item.Code == "AE-REPORT-SECTION-DATA-MISSING");
        Assert.Equal(fixture.ExpectedSections, report.Sections.Select(item => item.SectionKind.ToString()).ToArray());
    }

    [Fact]
    public void DuplicateDiagnosticsAreRemoved()
    {
        var request = EngineeringReportTestData.CreateMinimalRequest() with
        {
            ValidationDiagnostics =
            [
                new AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnostic(
                    AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnosticSeverity.Warning,
                    "AE-DUP",
                    "Duplicate diagnostic"),
                new AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnostic(
                    AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnosticSeverity.Warning,
                    "AE-DUP",
                    "Duplicate diagnostic")
            ]
        };

        var report = _builder.Build(request);
        Assert.Single(report.Diagnostics, item => item.Code == "AE-DUP");
    }

    [Fact]
    public void AssumptionsAndWarningsArePreserved()
    {
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        Assert.Contains(report.Assumptions, item => item.Contains("assumption", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Warnings, item => item.Contains("warning", StringComparison.OrdinalIgnoreCase));
    }
}
