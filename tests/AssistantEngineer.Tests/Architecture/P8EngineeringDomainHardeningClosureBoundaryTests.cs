using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8EngineeringDomainHardeningClosureBoundaryTests
{
    [Fact]
    public void ClosureArtifactsDoNotContainForbiddenPositiveClaims()
    {
        var files = new[]
        {
            ClosureMarkdownPath,
            ClosureJsonPath
        };

        var forbidden = new[]
        {
            "full " + "pybuilding" + "energy parity",
            "energy" + "plus parity",
            "ashrae " + "140 validated",
            "iso certified",
            "full tenant isolation complete",
            "production security certified",
            "ownership backfill executed",
            "production apply enabled"
        };

        var violations = GovernanceClaimTestHelper.FindForbiddenPhraseViolations(files, forbidden);
        Assert.True(violations.Count == 0, "Forbidden positive closure claims found:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void ClosureJsonDeclaresNoBehaviorChanges()
    {
        using var document = GovernanceJsonTestHelper.Parse(ClosureJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "publicApiChanged",
                "dtoShapesChanged",
                "authorizationSemanticsChanged",
                "workflowBehaviorChanged",
                "calculationPhysicsChanged",
                "ownershipBackfillApplyEnabled"
            ]);
    }

    [Fact]
    public void SecurityReleaseBoundaryRemainsDisabled()
    {
        using var document = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-release-boundary.json"));
        var releaseBoundary = document.RootElement.GetProperty("releaseBoundary");

        GovernanceAssertions.AssertReleaseBoundaryDisabled(
            releaseBoundary,
            [
                "productionApplyEnabled",
                "stagingApplyEnabled",
                "ownershipBackfillExecuted",
                "dbWritePathEnabled",
                "globalEfQueryFiltersEnabled",
                "databaseRowLevelSecurityEnabled",
                "fullTenantIsolationClaimed",
                "productionSecurityCertified"
            ]);
    }

    [Fact]
    public void GuardrailsStillReferenceApplyDisabledBoundaryTests()
    {
        using var document = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));

        var guardrails = document.RootElement.GetProperty("guardrails").EnumerateArray().ToArray();
        Assert.Contains(
            guardrails,
            item => string.Equals(item.GetProperty("guardrailId").GetString(), "SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-DESIGN-DISABLED", StringComparison.Ordinal));
        Assert.Contains(
            guardrails,
            item => string.Equals(item.GetProperty("guardrailId").GetString(), "SEC-GUARD-OWNERSHIP-BACKFILL-CLI-UX", StringComparison.Ordinal));
    }

    private static string ClosureMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p8-engineering-domain-hardening-closure.md");

    private static string ClosureJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p8-engineering-domain-hardening-closure.json");
}
