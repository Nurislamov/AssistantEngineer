using System.Text.Json;
using AssistantEngineer.Modules.Reporting.Application.Services;

namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportJsonExporterTests
{
    private readonly EngineeringReportBuilder _builder = new(
        new FixedTimeProvider(EngineeringReportTestData.FixedTimestamp),
        new EngineeringReportDiagnosticAggregator());

    private readonly EngineeringReportJsonExporter _exporter = new();

    [Fact]
    public void ExportsValidJsonWithSchemaVersionAndRequiredFields()
    {
        var fixture = EngineeringReportFixtureLoader.Load("json-export-fixture.json");
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var json = _exporter.Export(report, indented: false);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("SchemaVersion").GetString());
        Assert.True(root.TryGetProperty("ReportId", out _));
        Assert.True(root.TryGetProperty("Sections", out _));
        Assert.True(root.TryGetProperty("Diagnostics", out _));

        var sectionKinds = root.GetProperty("Sections")
            .EnumerateArray()
            .Select(item => item.GetProperty("SectionKind").GetString())
            .ToArray();

        foreach (var expected in fixture.ExpectedSections)
            Assert.Contains(expected, sectionKinds);
    }

    [Fact]
    public void SectionOrderIsStableInJsonOutput()
    {
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var left = _exporter.Export(report, indented: false);
        var right = _exporter.Export(report, indented: false);
        Assert.Equal(left, right);
    }

    [Fact]
    public void DoesNotWriteGeneratedFilesByDefault()
    {
        var generatedDirectory = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "reporting", "generated");
        Assert.False(Directory.Exists(generatedDirectory), $"Generated report artifacts must not be written by default: {generatedDirectory}");
    }

    [Fact]
    public void TraceAppendixIsIncludedWhenRequested()
    {
        var report = _builder.Build(EngineeringReportTestData.CreateFullRequest(includeTrace: true));
        var json = _exporter.Export(report, indented: false);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("TraceAppendix", out var traceNode));
        Assert.Equal("trace-report-001", traceNode.GetProperty("TraceId").GetString());
    }
}
