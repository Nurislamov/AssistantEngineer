namespace AssistantEngineer.Tests.Calculations.Trace;

public sealed class CalculationTraceDocumentationTests
{
    [Fact]
    public void DocsMentionSupportedModulesAndDetailLevels()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "calculation-trace.md");

        Assert.True(File.Exists(docPath), $"Calculation trace documentation file was not found: {docPath}");

        var text = File.ReadAllText(docPath);
        Assert.Contains("Weather", text, StringComparison.Ordinal);
        Assert.Contains("Solar", text, StringComparison.Ordinal);
        Assert.Contains("ThermalTopology", text, StringComparison.Ordinal);
        Assert.Contains("Iso52016", text, StringComparison.Ordinal);
        Assert.Contains("MultiZone", text, StringComparison.Ordinal);
        Assert.Contains("Ventilation", text, StringComparison.Ordinal);
        Assert.Contains("Ground", text, StringComparison.Ordinal);
        Assert.Contains("DomesticHotWater", text, StringComparison.Ordinal);
        Assert.Contains("SystemEnergy", text, StringComparison.Ordinal);

        Assert.Contains("None", text, StringComparison.Ordinal);
        Assert.Contains("Summary", text, StringComparison.Ordinal);
        Assert.Contains("Standard", text, StringComparison.Ordinal);
        Assert.Contains("Detailed", text, StringComparison.Ordinal);
        Assert.Contains("Debug", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DocsMentionKnownLimitationsAndNonClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "calculation-trace.md");
        var text = File.ReadAllText(docPath);

        Assert.Contains("internal engineering calculations only", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a legal compliance certificate", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not external validation evidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not prove full standard compliance", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("debug trace may be incomplete", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no PDF/HTML rendering", text, StringComparison.OrdinalIgnoreCase);
    }
}
