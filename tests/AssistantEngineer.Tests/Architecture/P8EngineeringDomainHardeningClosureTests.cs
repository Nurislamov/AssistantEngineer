using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8EngineeringDomainHardeningClosureTests
{
    [Fact]
    public void ClosureArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            ClosureMarkdownPath,
            ClosureJsonPath,
            ClosureSchemaPath);
    }

    [Fact]
    public void ClosureJsonParsesAndBoundaryFlagsRemainDisabled()
    {
        using var document = GovernanceJsonTestHelper.Parse(ClosureJsonPath);
        var root = document.RootElement;

        Assert.Equal("ClosedWithDeferredBacklog", root.GetProperty("p8ClosureStatus").GetString());

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "publicApiChanged",
                "dtoShapesChanged",
                "authorizationSemanticsChanged",
                "workflowBehaviorChanged",
                "calculationPhysicsChanged",
                "ownershipBackfillApplyEnabled",
                "dbWritePathEnabled",
                "globalEfQueryFiltersEnabled",
                "databaseRowLevelSecurityEnabled"
            ]);
    }

    [Fact]
    public void ClosureJsonIncludesP8StagesAndP9Backlog()
    {
        using var document = GovernanceJsonTestHelper.Parse(ClosureJsonPath);
        var root = document.RootElement;

        var completed = root.GetProperty("stagesCompleted")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var expectedStages = new[]
        {
            "P8-00", "P8-01", "P8-02", "P8-03A", "P8-03B", "P8-03C", "P8-03D", "P8-03E", "P8-03F",
            "P8-04", "P8-05", "P8-06", "P8-07", "P8-08", "P8-09"
        };

        foreach (var stage in expectedStages)
            Assert.Contains(stage, completed);

        Assert.NotEmpty(root.GetProperty("recommendedP9Backlog").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("nonClaims").EnumerateArray());
    }

    [Fact]
    public void ReadinessInventoryAndGuardrailsContainP8_09ClosureReferences()
    {
        using var readiness = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmap = readiness.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P8-09", roadmap);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var ids = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-P8-ENGINEERING-DOMAIN-CLOSURE", ids);
    }

    private static string ClosureMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p8-engineering-domain-hardening-closure.md");

    private static string ClosureJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p8-engineering-domain-hardening-closure.json");

    private static string ClosureSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p8-engineering-domain-hardening-closure.schema.json");
}
