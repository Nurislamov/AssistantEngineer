using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8ScriptsToolsRationalizationTests
{
    [Fact]
    public void RationalizationArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            RationalizationMarkdownPath,
            RationalizationJsonPath,
            RationalizationSchemaPath);
    }

    [Fact]
    public void RationalizationJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(RationalizationJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "releaseGateSemanticsChanged",
                "calculationPhysicsChanged",
                "publicApiChanged",
                "scriptsRemoved",
                "toolsRemoved"
            ]);

        Assert.NotEmpty(root.GetProperty("entriesReviewed").EnumerateArray());
    }

    [Fact]
    public void RationalizationJsonContainsRequiredNonClaims()
    {
        using var document = GovernanceJsonTestHelper.Parse(RationalizationJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No release gate semantics change claim",
                "No ownership backfill execution claim",
                "No production apply enabled claim"
            ]);
    }

    [Fact]
    public void P8AuditMarksScriptsToolsFindingAsAddressedOrPartiallyAddressed()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .Single(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F06", StringComparison.Ordinal));

        Assert.Contains(
            finding.GetProperty("resolutionStatus").GetString(),
            new[] { "Addressed", "PartiallyAddressed", "InProgress" });
        Assert.Equal("P8-06", finding.GetProperty("resolutionStage").GetString());
    }

    private static string RationalizationMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-rationalization.md");

    private static string RationalizationJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-rationalization.json");

    private static string RationalizationSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-rationalization.schema.json");
}
