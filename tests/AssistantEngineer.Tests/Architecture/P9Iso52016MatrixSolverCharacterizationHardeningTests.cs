using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016MatrixSolverCharacterizationHardeningTests
{
    [Fact]
    public void HardeningArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            HardeningMarkdownPath,
            HardeningJsonPath,
            HardeningSchemaPath);
    }

    [Fact]
    public void HardeningJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(HardeningJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "fixtureNumericValuesChanged",
                "publicApiChanged",
                "validationClaimChanged",
                "tolerancePolicyChanged",
                "calculationSourceFilesChanged"
            ]);
    }

    [Fact]
    public void HardeningJsonContainsTestsSeamsAndInvariants()
    {
        using var document = GovernanceJsonTestHelper.Parse(HardeningJsonPath);
        var root = document.RootElement;

        Assert.NotEmpty(root.GetProperty("newCharacterizationTests").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("seamsCovered").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("invariantsPinned").EnumerateArray());

        var testNames = root.GetProperty("newCharacterizationTests")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("Iso52016MatrixAssemblyInvariantTests", testNames);
        Assert.Contains("Iso52016LoadVectorCharacterizationTests", testNames);
        Assert.Contains("Iso52016SolverKernelCharacterizationTests", testNames);
    }

    [Fact]
    public void NonClaimsContainRequiredBoundaries()
    {
        using var document = GovernanceJsonTestHelper.Parse(HardeningJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No calculation physics change claim",
                "No expected value change claim",
                "No fixture numeric value change claim",
                "No EnergyPlus " + "parity claim",
                "No ISO certification claim"
            ]);
    }

    [Fact]
    public void ReadinessInventoryAndGuardrailsContainP9_01B1References()
    {
        using var readiness = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmapItems = readiness.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P9-01B1", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var ids = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-ISO52016-MATRIX-SOLVER-CHARACTERIZATION-HARDENING", ids);
    }

    private static string HardeningMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.md");

    private static string HardeningJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.json");

    private static string HardeningSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.schema.json");
}
