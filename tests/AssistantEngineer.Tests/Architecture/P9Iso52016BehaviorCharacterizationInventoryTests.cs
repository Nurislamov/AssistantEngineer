using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016BehaviorCharacterizationInventoryTests
{
    [Fact]
    public void InventoryArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            InventoryMarkdownPath,
            InventoryJsonPath,
            InventorySchemaPath);
    }

    [Fact]
    public void InventoryJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
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
                "fixtureExpectedValueFilesChanged"
            ]);
    }

    [Fact]
    public void ComponentCoverageIncludesAllP9_01Components()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var coveredIds = inventory.RootElement.GetProperty("componentCoverage")
            .EnumerateArray()
            .Select(item => item.GetProperty("componentId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(coveredIds);

        using var componentMap = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.json"));
        var componentIds = componentMap.RootElement.GetProperty("components")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString() ?? string.Empty)
            .ToArray();

        foreach (var componentId in componentIds)
            Assert.Contains(componentId, coveredIds);
    }

    [Fact]
    public void CharacterizationTestsOrGapsAndTolerancePolicyArePresent()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;

        var testCount = root.GetProperty("characterizationTests").EnumerateArray().Count();
        var gapCount = root.GetProperty("gapsRetained").EnumerateArray().Count();

        Assert.True(testCount > 0 || gapCount > 0);
        Assert.NotEmpty(root.GetProperty("tolerancePolicy").EnumerateArray());
        Assert.True(root.TryGetProperty("hardeningReportReference", out var hardeningReference));
        Assert.Contains("iso52016-matrix-solver-characterization-hardening", hardeningReference.GetString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonClaimsContainRequiredBoundaries()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
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
    public void ReadinessInventoryAndGuardrailsContainP9_01AReferences()
    {
        using var readiness = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmapItems = readiness.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P9-01A", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var ids = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-ISO52016-BEHAVIOR-CHARACTERIZATION", ids);
        Assert.Contains("SEC-GUARD-ISO52016-MATRIX-SOLVER-CHARACTERIZATION-HARDENING", ids);
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.schema.json");
}
