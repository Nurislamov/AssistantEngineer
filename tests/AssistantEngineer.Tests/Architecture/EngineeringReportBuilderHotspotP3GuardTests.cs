namespace AssistantEngineer.Tests.Architecture;

public class EngineeringReportBuilderHotspotP3GuardTests
{
    [Fact]
    public void EngineeringReportBuilder_RemainsFacadeSizedAfterP3Phase3()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Reporting",
            "Application",
            "Services",
            "EngineeringReportBuilder.cs");

        Assert.True(File.Exists(path), $"Report builder file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 500,
            $"EngineeringReportBuilder must remain a focused orchestration facade after P3-09. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 500.");
    }

    [Fact]
    public void EngineeringReportBuilder_UsesExtractedSectionAndFormattingComponents()
    {
        var servicesPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Reporting",
            "Application",
            "Services");

        var builderPath = Path.Combine(servicesPath, "EngineeringReportBuilder.cs");
        var builderSource = File.ReadAllText(builderPath);

        Assert.True(File.Exists(Path.Combine(servicesPath, "EngineeringReportSectionBuilder.cs")));
        Assert.True(File.Exists(Path.Combine(servicesPath, "EngineeringReportDiagnosticsSectionBuilder.cs")));
        Assert.True(File.Exists(Path.Combine(servicesPath, "EngineeringReportFormattingService.cs")));

        Assert.Contains("EngineeringReportSectionBuilder", builderSource, StringComparison.Ordinal);
        Assert.Contains("EngineeringReportDiagnosticsSectionBuilder", builderSource, StringComparison.Ordinal);
        Assert.Contains("EngineeringReportFormattingService", builderSource, StringComparison.Ordinal);
    }
}
