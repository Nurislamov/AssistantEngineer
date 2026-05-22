using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8OwnershipBackfillCliParserSimplificationTests
{
    [Fact]
    public void SimplificationArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            SimplificationMarkdownPath,
            SimplificationJsonPath,
            SimplificationSchemaPath);
    }

    [Fact]
    public void SimplificationJsonParses_AndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(SimplificationJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("cliSemanticsChanged").GetBoolean());
        Assert.False(root.GetProperty("applyEnabled").GetBoolean());
        Assert.False(root.GetProperty("dbWritePathEnabled").GetBoolean());
        Assert.False(root.GetProperty("commandsChanged").GetBoolean());
        Assert.False(root.GetProperty("exitCodesChanged").GetBoolean());
        Assert.False(root.GetProperty("redactionChanged").GetBoolean());
    }

    [Fact]
    public void SimplificationJson_ListsExtractedParserComponents()
    {
        using var document = GovernanceJsonTestHelper.Parse(SimplificationJsonPath);
        var components = document.RootElement.GetProperty("parserComponentsAdded")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillCommandDescriptor.cs", components);
        Assert.Contains("tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillCommandDescriptorCatalog.cs", components);
        Assert.Contains("tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillArgumentReader.cs", components);
    }

    [Fact]
    public void SimplificationJson_ContainsRequiredNonClaims()
    {
        using var document = GovernanceJsonTestHelper.Parse(SimplificationJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, item => item.Contains("No ownership backfill execution claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, item => item.Contains("No production apply enabled claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, item => item.Contains("No DB write-path enabled claim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void P8AuditMarksParserFindingAsAddressedOrPartiallyAddressed()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .Single(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F04", StringComparison.Ordinal));

        Assert.Contains(
            finding.GetProperty("resolutionStatus").GetString(),
            new[] { "Addressed", "PartiallyAddressed", "InProgress" });
        Assert.Equal("P8-04", finding.GetProperty("resolutionStage").GetString());
    }

    private static string SimplificationMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "ownershipbackfill-cli-parser-simplification.md");

    private static string SimplificationJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "ownershipbackfill-cli-parser-simplification.json");

    private static string SimplificationSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "ownershipbackfill-cli-parser-simplification.schema.json");
}
