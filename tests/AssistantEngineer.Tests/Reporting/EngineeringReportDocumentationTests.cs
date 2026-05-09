namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportDocumentationTests
{
    [Fact]
    public void DocumentationMentionsSupportedReportKindsAndFormats()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reporting",
            "engineering-reports.md");

        Assert.True(File.Exists(docPath), $"Engineering report documentation file was not found: {docPath}");

        var text = File.ReadAllText(docPath);
        Assert.Contains("CalculationSummary", text, StringComparison.Ordinal);
        Assert.Contains("AnnualEnergy", text, StringComparison.Ordinal);
        Assert.Contains("HeatingCoolingLoad", text, StringComparison.Ordinal);
        Assert.Contains("DomesticHotWater", text, StringComparison.Ordinal);
        Assert.Contains("SystemEnergy", text, StringComparison.Ordinal);
        Assert.Contains("FullEngineeringCore", text, StringComparison.Ordinal);
        Assert.Contains("Json", text, StringComparison.Ordinal);
        Assert.Contains("Markdown", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentationMentionsTraceAppendixPartialBehaviorAndLimitations()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reporting",
            "engineering-reports.md");

        var text = File.ReadAllText(docPath);
        Assert.Contains("trace appendix", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("partial report", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("internal engineering calculations only", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a legal compliance certificate", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not external validation evidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not prove full standard compliance", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PDF/HTML production rendering is not the focus", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("charts are placeholders", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentationDoesNotAddUnsupportedComplianceClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reporting",
            "engineering-reports.md");

        var text = File.ReadAllText(docPath);
        Assert.DoesNotContain("full compliance certificate", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("official compliance proof", text, StringComparison.OrdinalIgnoreCase);
    }
}

