using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8GovernanceTestBrittlenessReductionTests
{
    [Fact]
    public void BrittlenessReductionArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            ReportMarkdownPath,
            ReportJsonPath,
            ReportSchemaPath);
    }

    [Fact]
    public void BrittlenessReductionJsonParsesAndFlagsRemainNoChange()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReportJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            ["runtimeBehaviorChanged", "publicApiChanged", "calculationPhysicsChanged", "guardrailsWeakened"]);

        Assert.NotEmpty(root.GetProperty("testsRefactored").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("semanticChecksAdded").EnumerateArray());
    }

    [Fact]
    public void BrittlenessReductionNonClaimsRemainExplicit()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReportJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No runtime behavior change claim",
                "No public API route change claim",
                "No ownership backfill execution claim",
                "No production apply enabled claim"
            ]);
    }

    [Fact]
    public void P8AuditTracksGovernanceTestBrittlenessFinding()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .Single(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F07", StringComparison.Ordinal));

        Assert.Contains(
            finding.GetProperty("resolutionStatus").GetString(),
            new[] { "Addressed", "PartiallyAddressed", "InProgress" });
        Assert.Equal("P8-08", finding.GetProperty("resolutionStage").GetString());
    }

    private static string ReportMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "governance-test-brittleness-reduction.md");

    private static string ReportJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "governance-test-brittleness-reduction.json");

    private static string ReportSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "governance-test-brittleness-reduction.schema.json");
}
