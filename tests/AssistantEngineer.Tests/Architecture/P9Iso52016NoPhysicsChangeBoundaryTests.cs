using AssistantEngineer.Tests.Architecture.Governance;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016NoPhysicsChangeBoundaryTests
{
    [Fact]
    public void P9_01ReviewFlagsKeepPhysicsAndExpectedValuesUnchanged()
    {
        using var review = GovernanceJsonTestHelper.Parse(ReviewJsonPath);
        var root = review.RootElement;

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
    public void P9_01AInventoryFlagsKeepPhysicsAndExpectedValuesUnchanged()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.json"));

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            inventory.RootElement,
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
    public void P9_01DocsDoNotContainPositiveForbiddenClaims()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.json"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.json"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.json"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json")
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void ValidationRoadmapAndProvenanceRemainNoChangeForPhysicsAndExpectedValues()
    {
        using var roadmap = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.json"));
        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            roadmap.RootElement,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged", "validationClaimChanged"]);

        using var provenance = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.json"));
        Assert.True(provenance.RootElement.GetProperty("expectedValuesChanged").ValueKind == System.Text.Json.JsonValueKind.False);
    }

    [Fact]
    public void P9_01BDesignAndRiskRegisterKeepNoPhysicsAndNoExpectedValueChangeFlags()
    {
        using var design = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json"));
        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            design.RootElement,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "publicApiChanged",
                "validationClaimChanged",
                "calculationSourceFilesChanged",
                "fixtureExpectedValueFilesChanged"
            ]);

        using var riskRegister = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json"));
        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            riskRegister.RootElement,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "publicApiChanged",
                "validationClaimChanged"
            ]);
    }

    [Fact]
    public void P9_01B1HardeningKeepsNoPhysicsAndNoExpectedValueChangeFlags()
    {
        using var hardening = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.json"));
        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            hardening.RootElement,
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

    private static string ReviewJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.json");
}
