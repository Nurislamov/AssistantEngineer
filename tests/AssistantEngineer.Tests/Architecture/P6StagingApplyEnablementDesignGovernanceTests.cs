using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6StagingApplyEnablementDesignGovernanceTests
{
    [Fact]
    public void StagingRunbookAndChecklistArtifactsExist()
    {
        Assert.True(File.Exists(RunbookDocPath), $"Missing runbook doc: {RunbookDocPath}");
        Assert.True(File.Exists(RunbookJsonPath), $"Missing runbook json: {RunbookJsonPath}");
        Assert.True(File.Exists(RunbookSchemaPath), $"Missing runbook schema: {RunbookSchemaPath}");
        Assert.True(File.Exists(ChecklistMarkdownPath), $"Missing checklist markdown: {ChecklistMarkdownPath}");
        Assert.True(File.Exists(ChecklistJsonPath), $"Missing checklist json: {ChecklistJsonPath}");
    }

    [Fact]
    public void RunbookContainsRequiredSections()
    {
        var content = File.ReadAllText(RunbookDocPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Current status",
            "## Staging environment requirements",
            "## Staging operator policy",
            "## Required staging evidence chain",
            "## Staging command sequence, future only",
            "## Staging acceptance criteria",
            "## Staging failure handling",
            "## Promotion to production proposal"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RunbookContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(RunbookDocPath);
        var required = new[]
        {
            "No staging apply execution claim.",
            "No production apply enabled claim.",
            "No ownership backfill execution claim.",
            "No full multi-tenant isolation claim yet.",
            "No database row-level security claim.",
            "No global EF query filter claim.",
            "No production security certification claim.",
            "No certified/certification claim."
        };

        foreach (var entry in required)
        {
            Assert.Contains(entry, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RunbookJsonContainsDisabledFlagsAndRequirements()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RunbookJsonPath));
        var root = document.RootElement;

        Assert.False(root.GetProperty("stagingApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("productionApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());

        var requiredEvidence = root.GetProperty("requiredEvidence")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        var environmentRequirements = root.GetProperty("environmentRequirements")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        var operatorRequirements = root.GetProperty("operatorRequirements")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ApplyInputHash", requiredEvidence);
        Assert.Contains("PreviousValuesSnapshot", requiredEvidence);
        Assert.Contains("RollbackRehearsalPlan", requiredEvidence);
        Assert.Contains("BackupBeforeApply", environmentRequirements);
        Assert.Contains("RestoreProcedureVerified", environmentRequirements);
        Assert.Contains("ApplyInputHashReferenceRequired", operatorRequirements);
    }

    [Fact]
    public void ProductionInventoryContainsP6_10()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-10", items);
    }

    [Fact]
    public void GuardrailsContainStagingRunbookGuard()
    {
        var markdown = File.ReadAllText(GuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-APPLY-RUNBOOK", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardIds = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-APPLY-RUNBOOK", guardIds);
    }

    [Fact]
    public void StagingAcceptanceArtifactPatternsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);
        Assert.Contains("ownership-backfill-staging-acceptance-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-staging-acceptance-*.md", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyCommandRemainsDisabled()
    {
        var cli = File.ReadAllText(CliPath);
        Assert.Contains("Apply mode is designed but disabled", cli, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocsDoNotClaimStagingOrProductionApplyExecuted()
    {
        var docs = new[]
        {
            File.ReadAllText(RunbookDocPath),
            File.ReadAllText(ChecklistMarkdownPath),
            File.ReadAllText(ProposalDocPath),
            File.ReadAllText(InventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("staging apply has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("production apply is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");

    private static string RunbookDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-runbook.md");

    private static string RunbookJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-runbook.json");

    private static string RunbookSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-runbook.schema.json");

    private static string ChecklistMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-acceptance-checklist.md");

    private static string ChecklistJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-acceptance-checklist.json");

    private static string ProposalDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-apply-enablement-proposal.md");

    private static string GuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
