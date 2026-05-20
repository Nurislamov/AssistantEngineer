using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7SecurityGovernanceDocsConsistencyTests
{
    [Fact]
    public void SecurityJsonFilesAndMatchingSchemasParse()
    {
        var securityDirectory = Path.Combine(TestPaths.RepoRoot, "docs", "security");
        var jsonFiles = Directory.GetFiles(securityDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(jsonFiles);

        foreach (var jsonFile in jsonFiles)
            GovernanceJsonTestHelper.AssertJsonAndSchemaParseIfPresent(jsonFile);
    }

    [Fact]
    public void GovernanceIndexReferencesKeyP6DocsAndAllListedPathsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            SecurityGovernanceIndexMarkdownPath,
            SecurityGovernanceIndexJsonPath);

        using var index = GovernanceJsonTestHelper.Parse(SecurityGovernanceIndexJsonPath);
        var entries = index.RootElement.GetProperty("documents").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);

        var paths = entries
            .Select(entry => entry.GetProperty("path").GetString() ?? string.Empty)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.Ordinal);

        var requiredPaths = new[]
        {
            "docs/security/security-release-boundary.md",
            "docs/security/security-governance-status-vocabulary.md",
            "docs/security/ownership-backfill-strategy.md",
            "docs/security/ownership-backfill-dry-run-tool.md",
            "docs/security/ownership-backfill-database-dry-run-scanner.md",
            "docs/security/ownership-backfill-evidence-validation-gates.md",
            "docs/security/ownership-backfill-apply-plan-generator.md",
            "docs/security/ownership-backfill-plan-signoff-gate.md",
            "docs/security/ownership-backfill-apply-enablement-readiness.md",
            "docs/security/ownership-backfill-staging-apply-runbook.md",
            "docs/security/ownership-backfill-staging-apply-executor-design.md",
            "docs/security/ownership-backfill-staging-post-run-evidence.md",
            "docs/security/ownership-backfill-production-promotion-readiness.md",
            "docs/security/ownership-backfill-manual-write-path-enablement-decision.md",
            "docs/security/ownership-backfill-apply-enablement-architecture-review.md",
            "docs/security/post-p6-governance-audit.md",
            "docs/security/governance-test-consolidation-report.md",
            "docs/security/release-ready-observability-audit.md",
            "docs/security/ci-github-checks-visibility-audit.md",
            "docs/security/ci-github-checks-visibility-runbook.md",
            "docs/security/route-inventory-claims-consistency-audit.md",
            "docs/security/api-endpoint-classification-model.md",
            "docs/security/security-docs-map.md",
            "docs/adr/ADR-0001-security-governance-boundary.md",
            "docs/adr/security-architecture-decision-matrix.md",
            "docs/adr/security-architecture-decision-matrix.json",
            "docs/adr/future-security-adr-backlog.md",
            "docs/adr/future-security-adr-backlog.json",
            "docs/adr/adr-index.md",
            "docs/adr/adr-index.json"
        };

        foreach (var requiredPath in requiredPaths)
            Assert.Contains(requiredPath, paths);

        foreach (var path in paths)
        {
            var absolutePath = Path.Combine(TestPaths.RepoRoot, path.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(absolutePath), $"Missing file referenced by security-governance-index.json: {path}");
        }
    }

    [Fact]
    public void InventoryGuardrailsAndPostP6AuditContainP7_00Signals()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var inventoryItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P7-00", inventoryItems);
        Assert.Contains("P7-01", inventoryItems);
        Assert.Contains("P7-02", inventoryItems);
        Assert.Contains("P7-03", inventoryItems);
        Assert.Contains("P7-04", inventoryItems);
        Assert.Contains("P7-05", inventoryItems);
        Assert.Contains("P7-06", inventoryItems);
        Assert.Contains("P7-07", inventoryItems);
        Assert.Contains("P7-08", inventoryItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(GuardrailsJsonPath);
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-POST-P6-GOVERNANCE-AUDIT", guardrailIds);
        Assert.Contains("SEC-GUARD-SECURITY-GOVERNANCE-RELEASE-BOUNDARY", guardrailIds);
        Assert.Contains("SEC-GUARD-SECURITY-GOVERNANCE-INDEX-NORMALIZATION", guardrailIds);
        Assert.Contains("SEC-GUARD-GOVERNANCE-TEST-CONSOLIDATION", guardrailIds);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-CLI-UX", guardrailIds);
        Assert.Contains("SEC-GUARD-RELEASE-READY-OBSERVABILITY", guardrailIds);
        Assert.Contains("SEC-GUARD-CI-GITHUB-CHECKS-VISIBILITY", guardrailIds);
        Assert.Contains("SEC-GUARD-ROUTE-INVENTORY-CLAIMS-CONSISTENCY", guardrailIds);
        Assert.Contains("SEC-GUARD-SECURITY-DOCS-MAP-ADR", guardrailIds);
        Assert.Contains("SEC-GUARD-SECURITY-ADR-DECISION-MATRIX", guardrailIds);

        using var audit = GovernanceJsonTestHelper.Parse(PostP6AuditJsonPath);
        var releaseBoundary = audit.RootElement.GetProperty("releaseBoundary");
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
    }

    private static string SecurityGovernanceIndexMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.md");

    private static string SecurityGovernanceIndexJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.json");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string PostP6AuditJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "post-p6-governance-audit.json");
}
