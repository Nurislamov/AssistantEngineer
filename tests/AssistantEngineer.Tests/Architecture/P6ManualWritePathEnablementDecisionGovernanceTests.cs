using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6ManualWritePathEnablementDecisionGovernanceTests
{
    [Fact]
    public void ManualWritePathDecisionArtifactsExist()
    {
        Assert.True(File.Exists(ManualDecisionDocPath), $"Missing doc: {ManualDecisionDocPath}");
        Assert.True(File.Exists(ManualDecisionJsonPath), $"Missing json: {ManualDecisionJsonPath}");
        Assert.True(File.Exists(ManualDecisionSchemaPath), $"Missing schema: {ManualDecisionSchemaPath}");
        Assert.True(File.Exists(ManualDecisionTemplateMarkdownPath), $"Missing template md: {ManualDecisionTemplateMarkdownPath}");
        Assert.True(File.Exists(ManualDecisionTemplateJsonPath), $"Missing template json: {ManualDecisionTemplateJsonPath}");
    }

    [Fact]
    public void ManualDecisionDocContainsRequiredSections()
    {
        var content = File.ReadAllText(ManualDecisionDocPath);

        Assert.Contains("## Purpose", content, StringComparison.Ordinal);
        Assert.Contains("## Scope", content, StringComparison.Ordinal);
        Assert.Contains("## Non-claims", content, StringComparison.Ordinal);
        Assert.Contains("## Current status", content, StringComparison.Ordinal);
        Assert.Contains("## Required decision packet", content, StringComparison.Ordinal);
        Assert.Contains("## Manual approval policy", content, StringComparison.Ordinal);
        Assert.Contains("## Go/no-go review checklist", content, StringComparison.Ordinal);
        Assert.Contains("## Code enablement boundary", content, StringComparison.Ordinal);
        Assert.Contains("## Human-only decision log", content, StringComparison.Ordinal);
        Assert.Contains("## Future code enablement boundary", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualDecisionDocContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ManualDecisionDocPath);

        Assert.Contains("No production apply enabled claim.", content, StringComparison.Ordinal);
        Assert.Contains("No ownership backfill execution claim.", content, StringComparison.Ordinal);
        Assert.Contains("No global EF query filter claim.", content, StringComparison.Ordinal);
        Assert.Contains("No database row-level security claim.", content, StringComparison.Ordinal);
        Assert.Contains("No full multi-tenant isolation claim yet.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualDecisionJsonHasDisabledFlagsAndRequiredBindings()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ManualDecisionJsonPath));
        var root = document.RootElement;

        Assert.False(root.GetProperty("productionApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("stagingApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());
        Assert.False(root.GetProperty("codeWritePathEnabled").GetBoolean());

        var requiredHashes = root.GetProperty("requiredHashes")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("ProductionPromotionHash", requiredHashes);
        Assert.Contains("ApplyInputHash", requiredHashes);

        var requiredApprovals = root.GetProperty("requiredApprovals")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("DatabaseReleaseOwner", requiredApprovals);
        Assert.Contains("SecurityReviewer", requiredApprovals);
    }

    [Fact]
    public void InventoryAndGuardrailsContainP6_14()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var inventoryItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P6-14", inventoryItems);

        using var guardrails = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-MANUAL-WRITE-PATH-DECISION", guardrailIds);
    }

    [Fact]
    public void DocsDoNotClaimApplyEnabledBackfillExecutedOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(ManualDecisionDocPath),
            File.ReadAllText(ManualDecisionTemplateMarkdownPath),
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

    private static string ManualDecisionDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-manual-write-path-enablement-decision.md");

    private static string ManualDecisionJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-manual-write-path-enablement-decision.json");

    private static string ManualDecisionSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-manual-write-path-enablement-decision.schema.json");

    private static string ManualDecisionTemplateMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-manual-decision-log-template.md");

    private static string ManualDecisionTemplateJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-manual-decision-log-template.json");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");
}
