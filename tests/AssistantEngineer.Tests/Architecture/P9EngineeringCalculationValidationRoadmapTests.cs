using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9EngineeringCalculationValidationRoadmapTests
{
    [Fact]
    public void RoadmapArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            RoadmapMarkdownPath,
            RoadmapJsonPath,
            RoadmapSchemaPath);
    }

    [Fact]
    public void RoadmapJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(RoadmapJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "publicApiChanged",
                "validationClaimChanged"
            ]);
    }

    [Fact]
    public void CoverageAreasAndMaturityModelContainRequiredEntries()
    {
        using var document = GovernanceJsonTestHelper.Parse(RoadmapJsonPath);
        var root = document.RootElement;

        var areas = root.GetProperty("coverageAreas")
            .EnumerateArray()
            .Select(item => item.GetProperty("area").GetString() ?? string.Empty)
            .ToArray();

        Assert.NotEmpty(areas);
        Assert.Contains(areas, value => value.Contains("ISO52010", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(areas, value => value.Contains("ISO52016", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(areas, value => value.Contains("Heating/cooling load", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(areas, value => value.Contains("Reports/workflow", StringComparison.OrdinalIgnoreCase));

        var maturityLevels = root.GetProperty("maturityModel")
            .EnumerateArray()
            .Select(item => item.GetProperty("level").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var requiredLevels = new[]
        {
            "None",
            "SmokeOnly",
            "InternalInvariant",
            "ManualReferenceAnchor",
            "IndependentReferenceFixture",
            "ExternalToolReferenceFixture",
            "CrossImplementationComparison",
            "CandidateForFormalValidation"
        };

        foreach (var level in requiredLevels)
            Assert.Contains(level, maturityLevels);
    }

    [Fact]
    public void BacklogAndNonClaimsArePresent()
    {
        using var document = GovernanceJsonTestHelper.Parse(RoadmapJsonPath);
        var root = document.RootElement;

        Assert.NotEmpty(root.GetProperty("recommendedP9Backlog").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("nonClaims").EnumerateArray());
    }

    [Fact]
    public void ReadinessInventoryAndGuardrailsContainP9_00References()
    {
        using var readiness = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmapItems = readiness.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P9-00", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var ids = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-ENGINEERING-VALIDATION-ROADMAP", ids);
    }

    private static string RoadmapMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.md");

    private static string RoadmapJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.json");

    private static string RoadmapSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.schema.json");
}
