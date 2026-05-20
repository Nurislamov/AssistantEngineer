using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7CiGithubChecksVisibilityAuditTests
{
    [Fact]
    public void CiVisibilityAuditArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            AuditDocPath,
            AuditJsonPath,
            AuditSchemaPath);
    }

    [Fact]
    public void CiVisibilityAuditDocumentContainsRequiredSectionsAndNonClaims()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            AuditDocPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## Current GitHub checks visibility",
                "## Current workflow inventory",
                "## Expected CI visibility contract",
                "## Release-ready relationship",
                "## Required checks",
                "## Optional checks",
                "## GitHub status limitations",
                "## Observability gaps",
                "## Implemented improvements",
                "## Remaining limitations",
                "## Next steps"
            ]);

        GovernanceDocumentTestHelper.AssertMarkdownContainsPhrases(
            AuditDocPath,
            [
                "No production security certification claim.",
                "No ownership backfill execution claim.",
                "No production apply enabled claim.",
                "No staging apply execution claim."
            ]);
    }

    [Fact]
    public void CiVisibilityAuditJsonFlagsAndRequiredChecksAreValid()
    {
        using var document = GovernanceJsonTestHelper.Parse(AuditJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("releaseGateSemanticsChanged").GetBoolean());

        var requiredChecks = GovernanceJsonTestHelper.StringSet(root.GetProperty("requiredChecks"));
        Assert.Contains("dotnet build AssistantEngineer.sln -c Debug", requiredChecks);
        Assert.Contains("dotnet test AssistantEngineer.sln -c Debug", requiredChecks);
        Assert.Contains("scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1", requiredChecks);
    }

    [Fact]
    public void InventoryGuardrailsAndIndexContainP7_05Signals()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var items = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P7-05", items);

        using var guardrails = GovernanceJsonTestHelper.Parse(GuardrailsJsonPath);
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-CI-GITHUB-CHECKS-VISIBILITY", guardrailIds);

        using var index = GovernanceJsonTestHelper.Parse(IndexJsonPath);
        var indexPaths = index.RootElement.GetProperty("documents")
            .EnumerateArray()
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("docs/security/ci-github-checks-visibility-audit.md", indexPaths);
        Assert.Contains("docs/security/ci-github-checks-visibility-runbook.md", indexPaths);
    }

    [Fact]
    public void AuditDocDoesNotClaimAlwaysGreenCiOrEnabledWritePath()
    {
        var content = File.ReadAllText(AuditDocPath);
        Assert.DoesNotContain("all checks are green", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("production apply is enabled", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownership backfill was executed", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full tenant isolation complete", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string AuditDocPath =>
        GovernancePathHelper.SecurityDocPath("ci-github-checks-visibility-audit.md");

    private static string AuditJsonPath =>
        GovernancePathHelper.SecurityDocPath("ci-github-checks-visibility-audit.json");

    private static string AuditSchemaPath =>
        GovernancePathHelper.SecurityDocPath("ci-github-checks-visibility-audit.schema.json");

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json");

    private static string GuardrailsJsonPath =>
        GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json");

    private static string IndexJsonPath =>
        GovernancePathHelper.SecurityDocPath("security-governance-index.json");
}
