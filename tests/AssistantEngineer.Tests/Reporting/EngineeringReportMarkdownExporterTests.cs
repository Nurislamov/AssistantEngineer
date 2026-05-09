using AssistantEngineer.Modules.Reporting.Application.Services;

namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportMarkdownExporterTests
{
    private readonly EngineeringReportBuilder _builder = new(
        new FixedTimeProvider(EngineeringReportTestData.FixedTimestamp),
        new EngineeringReportDiagnosticAggregator());

    private readonly EngineeringReportMarkdownExporter _exporter = new();

    [Fact]
    public void MarkdownContainsExpectedHeadingsAndTables()
    {
        var fixture = EngineeringReportFixtureLoader.Load("markdown-export-fixture.json");
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var markdown = _exporter.Export(report);

        foreach (var heading in fixture.ExpectedHeadings ?? [])
            Assert.Contains(heading, markdown, StringComparison.Ordinal);

        foreach (var column in fixture.ExpectedTableColumns ?? [])
            Assert.Contains(column, markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkdownContainsWarningsAssumptionsAndLimitations()
    {
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var markdown = _exporter.Export(report);

        Assert.Contains("## Report Assumptions", markdown, StringComparison.Ordinal);
        Assert.Contains("## Report Warnings", markdown, StringComparison.Ordinal);
        Assert.Contains("Known Limitations", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void TraceAppendixIsSummarizedInStandardMode()
    {
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var markdown = _exporter.Export(report);

        Assert.Contains("Calculation Trace Appendix", markdown, StringComparison.Ordinal);
        Assert.Contains("Trace step summary", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkdownDoesNotAddUnsupportedComplianceClaims()
    {
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var markdown = _exporter.Export(report);

        Assert.DoesNotContain("full compliance", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("official compliance proof", markdown, StringComparison.OrdinalIgnoreCase);
    }
}
