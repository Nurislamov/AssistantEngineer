using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6ProductionApplyEnablementProposalGovernanceTests
{
    [Fact]
    public void ProposalAndTemplateArtifactsExist()
    {
        Assert.True(File.Exists(ProposalDocPath), $"Missing proposal doc: {ProposalDocPath}");
        Assert.True(File.Exists(ProposalJsonPath), $"Missing proposal json: {ProposalJsonPath}");
        Assert.True(File.Exists(ProposalSchemaPath), $"Missing proposal schema: {ProposalSchemaPath}");
        Assert.True(File.Exists(ChangeTemplateMarkdownPath), $"Missing change-request template markdown: {ChangeTemplateMarkdownPath}");
        Assert.True(File.Exists(ChangeTemplateJsonPath), $"Missing change-request template json: {ChangeTemplateJsonPath}");
    }

    [Fact]
    public void ProposalDocContainsRequiredSections()
    {
        var content = File.ReadAllText(ProposalDocPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Current status",
            "## Required evidence chain",
            "## Approval policy",
            "## Environment separation",
            "## Backup readiness",
            "## Rollback readiness",
            "## Go/no-go criteria",
            "## Change-management template",
            "## Future enablement stages"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProposalDocContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ProposalDocPath);
        var required = new[]
        {
            "No ownership backfill execution claim.",
            "No production apply enabled claim.",
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
    public void ProposalJsonContainsDisabledStateAndRequiredFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProposalJsonPath));
        var root = document.RootElement;

        Assert.False(root.GetProperty("applyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());

        var requiredEvidence = root.GetProperty("requiredEvidence")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        var changeFields = root.GetProperty("changeManagementFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        var goCriteria = root.GetProperty("goCriteria")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        var noGoCriteria = root.GetProperty("noGoCriteria")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ApplyInputHash", requiredEvidence);
        Assert.Contains("ApplyInputHash", changeFields);
        Assert.Contains("BackupAndRestoreVerified", goCriteria);
        Assert.Contains("MissingPreviousValues", noGoCriteria);
        Assert.Contains("BackupMissing", noGoCriteria);
        Assert.Contains("RestoreUnverified", noGoCriteria);
    }

    [Fact]
    public void ProductionInventoryContainsP6_09()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-09", items);
    }

    [Fact]
    public void GuardrailsContainP6_09ProposalGuard()
    {
        var markdown = File.ReadAllText(GuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-PRODUCTION-APPLY-PROPOSAL", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var ids = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-PRODUCTION-APPLY-PROPOSAL", ids);
    }

    [Fact]
    public void ApplyCommandRemainsDisabled()
    {
        var cli = File.ReadAllText(CliPath);
        Assert.Contains("Apply mode is designed but disabled", cli, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocsDoNotClaimApplyEnabledBackfillExecutedOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(ProposalDocPath),
            File.ReadAllText(ChangeTemplateMarkdownPath),
            File.ReadAllText(InventoryMarkdownPath),
            File.ReadAllText(ApplyDesignDocPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("production apply is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill is fully complete", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");

    private static string ProposalDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-apply-enablement-proposal.md");

    private static string ProposalJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-apply-enablement-proposal.json");

    private static string ProposalSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-apply-enablement-proposal.schema.json");

    private static string ChangeTemplateMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-change-request-template.md");

    private static string ChangeTemplateJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-change-request-template.json");

    private static string GuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string ApplyDesignDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.md");
}
