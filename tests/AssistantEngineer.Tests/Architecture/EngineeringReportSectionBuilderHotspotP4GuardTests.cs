namespace AssistantEngineer.Tests.Architecture;

public sealed class EngineeringReportSectionBuilderHotspotP4GuardTests
{
    [Fact]
    public void EngineeringReportSectionBuilder_RemainsThinOrchestratorAfterP4Split()
    {
        var sectionBuilderPath = Resolve(
            "src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportSectionBuilder.cs");

        Assert.True(File.Exists(sectionBuilderPath), $"Section builder file was not found: {sectionBuilderPath}");

        var nonBlankLines = File.ReadAllLines(sectionBuilderPath).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 220,
            $"EngineeringReportSectionBuilder should stay thin after hotspot split. Current non-blank lines: {nonBlankLines}; expected <= 220.");
    }

    [Fact]
    public void EngineeringReportSectionBuilders_ExistForFocusedFamilies()
    {
        var expectedFiles = new[]
        {
            "src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportExecutiveSummarySectionBuilder.cs",
            "src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportInputSummarySectionBuilder.cs",
            "src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportWeatherSolarSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportLoadResultsSectionBuilder.cs",
            "src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportSystemEnergySectionBuilder.cs",
            "src/Backend/AssistantEngineer.Modules.Reporting/Application/Services/EngineeringReportGovernanceMetadataSectionBuilder.cs"
        };

        foreach (var relativePath in expectedFiles)
        {
            Assert.True(File.Exists(Resolve(relativePath)), $"Expected focused report section builder file to exist: {relativePath}");
        }
    }

    private static string Resolve(string relativePath) =>
        Path.Combine(relativePath.Split('/').Prepend(TestPaths.RepoRoot).ToArray());
}
