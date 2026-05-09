using AssistantEngineer.Modules.Reporting;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;
using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportIntegrationTests
{
    private readonly EngineeringReportBuilder _builder = new(
        new FixedTimeProvider(EngineeringReportTestData.FixedTimestamp),
        new EngineeringReportDiagnosticAggregator());

    [Fact]
    public void CalculationTraceCanBeEmbeddedAndSummarized()
    {
        var fixture = EngineeringReportFixtureLoader.Load("trace-appendix-report.json");
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));

        Assert.NotNull(report.TraceAppendix);
        var sectionKinds = report.Sections.Select(item => item.SectionKind.ToString()).ToArray();
        foreach (var expected in fixture.ExpectedSections)
            Assert.Contains(expected, sectionKinds);
        Assert.Contains(report.Diagnostics, item => item.Code == "AE-TRACE-WARN");
    }

    [Fact]
    public void DhwAndSystemEnergySummariesMapIntoReportSections()
    {
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));

        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.DomesticHotWater);
        Assert.Contains(report.Sections, item => item.SectionKind == EngineeringReportSectionKind.SystemEnergy);
        Assert.Contains(report.Summaries, item => item.Key == "dhw_system_load_kwh");
        Assert.Contains(report.Summaries, item => item.Key == "final_energy_total_kwh");
    }

    [Fact]
    public void ValidationDiagnosticsAggregateDeterministically()
    {
        var request = EngineeringReportTestData.CreateMinimalRequest() with
        {
            ValidationDiagnostics =
            [
                new AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnostic(
                    AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnosticSeverity.Warning,
                    "AE-B",
                    "Diagnostic B"),
                new AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnostic(
                    AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics.CalculationDiagnosticSeverity.Error,
                    "AE-A",
                    "Diagnostic A")
            ]
        };

        var report = _builder.Build(request);
        var orderedCodes = report.Diagnostics
            .Where(item => item.Code is "AE-A" or "AE-B")
            .Select(item => item.Code)
            .ToArray();
        Assert.Equal(["AE-A", "AE-B"], orderedCodes);
    }

    [Fact]
    public void ReportingDependencyInjectionResolvesEngineeringReportServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(EngineeringReportTestData.FixedTimestamp));
        services.AddReportingModule();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IEngineeringReportBuilder>());
        Assert.NotNull(provider.GetService<IEngineeringReportJsonExporter>());
        Assert.NotNull(provider.GetService<IEngineeringReportMarkdownExporter>());
        Assert.NotNull(provider.GetService<IEngineeringReportDiagnosticAggregator>());
    }
}
