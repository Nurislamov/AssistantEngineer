using AssistantEngineer.Modules.Calculations.Application.Services.Governance;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016SeamDesignNoPhysicsChangeTests
{
    [Fact]
    public void SeamDesignFlagsKeepPhysicsExpectedValuesAndSourceChangesDisabled()
    {
        using var design = GovernanceJsonTestHelper.Parse(DesignJsonPath);
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

        var stages = design.RootElement.GetProperty("proposedExtractionStages").EnumerateArray().ToArray();
        Assert.NotEmpty(stages);
        Assert.All(stages, stage => Assert.False(stage.GetProperty("behaviorChangeAllowed").GetBoolean()));
    }

    [Fact]
    public void RiskRegisterKeepsExpectedValueChangeDisallowed()
    {
        using var riskRegister = GovernanceJsonTestHelper.Parse(RiskRegisterJsonPath);
        Assert.False(riskRegister.RootElement.GetProperty("expectedValueChangeAllowed").GetBoolean());
        Assert.All(
            riskRegister.RootElement.GetProperty("risks").EnumerateArray().ToArray(),
            risk => Assert.False(risk.GetProperty("expectedValueChangeAllowed").GetBoolean()));
    }

    [Fact]
    public void P9_01BDocsDoNotContainPositiveForbiddenClaims()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.json")
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string DesignJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json");

    private static string RiskRegisterJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json");
}
