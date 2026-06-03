using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016DecompositionReviewTests
{
    [Fact]
    public void ReviewArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            ReviewMarkdownPath,
            ReviewJsonPath,
            ReviewSchemaPath);
    }

    [Fact]
    public void ReviewJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReviewJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "publicApiChanged",
                "validationClaimChanged",
                "calculationSourceFilesChanged",
                "fixtureFilesChanged",
                "expectedValueFilesChanged"
            ]);
    }

    [Fact]
    public void ComponentsHotspotsCandidatesAndBacklogArePresent()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReviewJsonPath);
        var root = document.RootElement;

        Assert.NotEmpty(root.GetProperty("components").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("hotspots").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("decompositionCandidates").EnumerateArray());

        var stages = root.GetProperty("proposedBacklog")
            .EnumerateArray()
            .Select(item => item.GetProperty("stage").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var stage in new[] { "P9-01A", "P9-01B", "P9-01C", "P9-01D", "P9-01E", "P9-01F" })
            Assert.Contains(stage, stages);
    }

    [Fact]
    public void NonClaimsContainRequiredBoundaries()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReviewJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No calculation physics change claim",
                "No expected value change claim",
                "No EnergyPlus " + "parity claim",
                "No ISO certification claim"
            ]);
    }

    [Fact]
    public void ReadinessInventoryAndGuardrailsContainP9_01References()
    {
        using var readiness = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmapItems = readiness.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P9-01", roadmapItems);
        Assert.Contains("P9-01A", roadmapItems);
        Assert.Contains("P9-01B", roadmapItems);
        Assert.Contains("P9-01B1", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var ids = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-ISO52016-DECOMPOSITION-REVIEW", ids);
        Assert.Contains("SEC-GUARD-ISO52016-BEHAVIOR-CHARACTERIZATION", ids);
        Assert.Contains("SEC-GUARD-ISO52016-MATRIX-SOLVER-SEAM-DESIGN", ids);
        Assert.Contains("SEC-GUARD-ISO52016-MATRIX-SOLVER-CHARACTERIZATION-HARDENING", ids);
    }

    private static string ReviewMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.md");

    private static string ReviewJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.json");

    private static string ReviewSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.schema.json");
}
