using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6ApplyEnablementArchitectureReviewGovernanceTests
{
    [Fact]
    public void ArchitectureReviewArtifactsExist()
    {
        Assert.True(File.Exists(ArchitectureReviewDocPath), $"Missing doc: {ArchitectureReviewDocPath}");
        Assert.True(File.Exists(ArchitectureReviewJsonPath), $"Missing json: {ArchitectureReviewJsonPath}");
        Assert.True(File.Exists(ArchitectureReviewSchemaPath), $"Missing schema: {ArchitectureReviewSchemaPath}");
        Assert.True(File.Exists(ChecklistDocPath), $"Missing checklist doc: {ChecklistDocPath}");
        Assert.True(File.Exists(ChecklistJsonPath), $"Missing checklist json: {ChecklistJsonPath}");
    }

    [Fact]
    public void ArchitectureReviewDocContainsRequiredSections()
    {
        var content = File.ReadAllText(ArchitectureReviewDocPath);

        Assert.Contains("## Purpose", content, StringComparison.Ordinal);
        Assert.Contains("## Scope", content, StringComparison.Ordinal);
        Assert.Contains("## Non-claims", content, StringComparison.Ordinal);
        Assert.Contains("## Current status", content, StringComparison.Ordinal);
        Assert.Contains("## Architecture enablement invariants", content, StringComparison.Ordinal);
        Assert.Contains("## Forbidden architecture changes", content, StringComparison.Ordinal);
        Assert.Contains("## Required architecture review checklist", content, StringComparison.Ordinal);
        Assert.Contains("## Future code enablement criteria", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureReviewDocContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ArchitectureReviewDocPath);

        Assert.Contains("No production apply enabled claim.", content, StringComparison.Ordinal);
        Assert.Contains("No ownership backfill execution claim.", content, StringComparison.Ordinal);
        Assert.Contains("No global EF query filter claim.", content, StringComparison.Ordinal);
        Assert.Contains("No database row-level security claim.", content, StringComparison.Ordinal);
        Assert.Contains("No full multi-tenant isolation claim yet.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureReviewJsonHasDisabledFlagsAndRequiredInvariants()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ArchitectureReviewJsonPath));
        var root = document.RootElement;

        Assert.False(root.GetProperty("productionApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("stagingApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());
        Assert.False(root.GetProperty("codeWritePathEnabled").GetBoolean());

        var invariants = root.GetProperty("architectureInvariants")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ApplyDisabledInvariant", invariants);
        Assert.Contains("EnvironmentHardDenyInvariant", invariants);
        Assert.Contains("HashChainInvariant", invariants);
        Assert.Contains("EvidenceCompletenessInvariant", invariants);
        Assert.Contains("RollbackCompletenessInvariant", invariants);
        Assert.Contains("NoSecretsInvariant", invariants);
        Assert.Contains("NoPayloadInvariant", invariants);
        Assert.Contains("NoDestructiveSqlInvariant", invariants);
        Assert.Contains("NoGlobalFilterInvariant", invariants);
        Assert.Contains("NoProductionWiringInvariant", invariants);
    }

    [Fact]
    public void InventoryAndGuardrailsContainP6_15()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var inventoryItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P6-15", inventoryItems);

        using var guardrails = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-ARCHITECTURE-REVIEW", guardrailIds);
    }

    [Fact]
    public void DocsDoNotClaimApplyEnabledBackfillExecutedOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(ArchitectureReviewDocPath),
            File.ReadAllText(ChecklistDocPath),
            File.ReadAllText(InventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("production apply is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ArchitectureReviewDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-enablement-architecture-review.md");

    private static string ArchitectureReviewJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-enablement-architecture-review.json");

    private static string ArchitectureReviewSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-enablement-architecture-review.schema.json");

    private static string ChecklistDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-architecture-review-checklist.md");

    private static string ChecklistJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-architecture-review-checklist.json");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");
}
