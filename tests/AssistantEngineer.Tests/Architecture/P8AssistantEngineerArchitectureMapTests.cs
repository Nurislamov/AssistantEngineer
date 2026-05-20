using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8AssistantEngineerArchitectureMapTests
{
    [Fact]
    public void ArchitectureMapArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            MapMarkdownPath,
            MapJsonPath,
            MapSchemaPath);
    }

    [Fact]
    public void ArchitectureMapDocumentContainsRequiredSections()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            MapMarkdownPath,
            [
                "## Purpose",
                "## Backend module map",
                "## Domain modules",
                "## Application services",
                "## Infrastructure services",
                "## API controllers",
                "## Tools",
                "## Scripts",
                "## Tests",
                "## Docs/governance",
                "## Known boundaries",
                "## Known boundary risks",
                "## Next review points",
                "## Non-claims"
            ]);
    }

    [Fact]
    public void ArchitectureMapJsonParsesWithRequiredCollections()
    {
        using var document = GovernanceJsonTestHelper.Parse(MapJsonPath);
        var root = document.RootElement;

        Assert.NotEmpty(root.GetProperty("modules").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("tools").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("scripts").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("testAreas").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("boundaryRules").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("knownBoundaryRisks").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("nonClaims").EnumerateArray());
    }

    private static string MapMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "assistantengineer-architecture-map.md");

    private static string MapJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "assistantengineer-architecture-map.json");

    private static string MapSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "assistantengineer-architecture-map.schema.json");
}
