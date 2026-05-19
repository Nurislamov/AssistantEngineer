using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6StagingPostRunEvidenceAcceptanceGovernanceTests
{
    [Fact]
    public void StagingPostRunEvidenceArtifactsExist()
    {
        Assert.True(File.Exists(PostRunEvidenceDocPath), $"Missing doc: {PostRunEvidenceDocPath}");
        Assert.True(File.Exists(PostRunEvidenceJsonPath), $"Missing json: {PostRunEvidenceJsonPath}");
        Assert.True(File.Exists(PostRunEvidenceSchemaPath), $"Missing schema: {PostRunEvidenceSchemaPath}");
    }

    [Fact]
    public void PostRunEvidenceJsonHasDisabledFlags()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(PostRunEvidenceJsonPath));
        var root = document.RootElement;

        Assert.False(root.GetProperty("stagingApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("productionApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());
    }

    [Fact]
    public void ValidateStagingAcceptanceCommandExists()
    {
        var parser = File.ReadAllText(ParserPath);
        Assert.Contains("validate-staging-acceptance", parser, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceArtifactPatternsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);
        Assert.Contains("ownership-backfill-staging-acceptance-result-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-staging-acceptance-result-*.md", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolSourceContainsNoSaveChangesAndNoDestructiveSql()
    {
        var sourceFiles = Directory.GetFiles(ToolDirectoryPath, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(sourceFiles);

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("SaveChanges(", content, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChangesAsync(", content, StringComparison.Ordinal);
            Assert.DoesNotContain("UPDATE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TRUNCATE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("INSERT INTO", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CrossDocsReferencePostRunEvidenceContract()
    {
        var runbook = File.ReadAllText(RunbookDocPath);
        var executorDesign = File.ReadAllText(ExecutorDesignDocPath);
        var proposal = File.ReadAllText(ProposalDocPath);

        Assert.Contains("ownership-backfill-staging-post-run-evidence.md", runbook, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-staging-post-run-evidence.md", executorDesign, StringComparison.Ordinal);
        Assert.Contains("accepted staging acceptance result", proposal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InventoryAndGuardrailsContainP6_12()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var inventoryItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P6-12", inventoryItems);

        using var guardrails = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardrailsIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-POST-RUN-EVIDENCE-ACCEPTANCE", guardrailsIds);
    }

    [Fact]
    public void DocsDoNotClaimExecutionOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(PostRunEvidenceDocPath),
            File.ReadAllText(InventoryMarkdownPath),
            File.ReadAllText(ProposalDocPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("staging apply has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("production apply is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string ParserPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCommandLineParser.cs");

    private static string PostRunEvidenceDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-post-run-evidence.md");

    private static string PostRunEvidenceJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-post-run-evidence.json");

    private static string PostRunEvidenceSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-post-run-evidence.schema.json");

    private static string RunbookDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-runbook.md");

    private static string ExecutorDesignDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-apply-executor-design.md");

    private static string ProposalDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-apply-enablement-proposal.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
