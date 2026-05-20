using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7PostP6GovernanceAuditTests
{
    [Fact]
    public void PostP6AuditAndIndexArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            PostP6AuditDocPath,
            PostP6AuditJsonPath,
            PostP6AuditSchemaPath,
            SecurityGovernanceIndexDocPath,
            SecurityGovernanceIndexJsonPath);
    }

    [Fact]
    public void PostP6AuditDocContainsRequiredSectionsAndNonClaims()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            PostP6AuditDocPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## Current release boundary",
                "## Findings summary",
                "## P5 audit findings",
                "## P6 audit findings",
                "## Claims audit",
                "## Release boundary risks",
                "## Recommended cleanup backlog"
            ]);

        var content = File.ReadAllText(PostP6AuditDocPath);
        GovernanceAssertions.AssertRequiredNonClaims(
            content,
            [
                "No production apply enabled claim.",
                "No ownership backfill execution claim.",
                "No global EF query filter claim.",
                "No database row-level security claim.",
                "No full multi-tenant isolation claim yet."
            ]);
    }

    [Fact]
    public void PostP6AuditJsonContainsDisabledBoundaryAndBacklog()
    {
        using var document = GovernanceJsonTestHelper.Parse(PostP6AuditJsonPath);
        var root = document.RootElement;

        var releaseBoundary = root.GetProperty("releaseBoundary");
        GovernanceAssertions.AssertReleaseBoundaryDisabled(
            releaseBoundary,
            [
                "applyEnabled",
                "stagingApplyEnabled",
                "productionApplyEnabled",
                "backfillExecution",
                "globalEfQueryFilters",
                "databaseRowLevelSecurity",
                "fullTenantIsolationClaim"
            ]);

        var backlog = root.GetProperty("recommendedBacklog").EnumerateArray().ToArray();
        Assert.NotEmpty(backlog);
    }

    [Fact]
    public void InventoryAndGuardrailsContainP7_00References()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var inventoryItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P7-00", inventoryItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(GuardrailsJsonPath);
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-POST-P6-GOVERNANCE-AUDIT", guardrailIds);
    }

    [Fact]
    public void DocsDoNotClaimApplyEnabledBackfillExecutedOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(PostP6AuditDocPath),
            File.ReadAllText(SecurityGovernanceIndexDocPath),
            File.ReadAllText(InventoryMarkdownPath)
        };

        var forbiddenPhrases = new[]
        {
            "production apply enabled",
            "ownership backfill executed",
            "global ef query filters enabled",
            "database row-level security enabled",
            "full tenant isolation complete"
        };

        foreach (var content in docs)
        {
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var lowerLine = line.ToLowerInvariant();
                foreach (var phrase in forbiddenPhrases)
                {
                    var matchIndex = lowerLine.IndexOf(phrase, StringComparison.Ordinal);
                    if (matchIndex < 0)
                        continue;

                    if (line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("Non-claims", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Assert.Fail($"Forbidden positive claim detected: {phrase} in line '{line.Trim()}'.");
                }
            }
        }
    }

    private static string PostP6AuditDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "post-p6-governance-audit.md");

    private static string PostP6AuditJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "post-p6-governance-audit.json");

    private static string PostP6AuditSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "post-p6-governance-audit.schema.json");

    private static string SecurityGovernanceIndexDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.md");

    private static string SecurityGovernanceIndexJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.json");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");
}
