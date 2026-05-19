using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6ProductionPromotionReadinessGovernanceTests
{
    [Fact]
    public void ProductionPromotionReadinessArtifactsExist()
    {
        Assert.True(File.Exists(PromotionDocPath), $"Missing doc: {PromotionDocPath}");
        Assert.True(File.Exists(PromotionJsonPath), $"Missing json: {PromotionJsonPath}");
        Assert.True(File.Exists(PromotionSchemaPath), $"Missing schema: {PromotionSchemaPath}");
    }

    [Fact]
    public void ValidateProductionPromotionCommandExists()
    {
        var parser = File.ReadAllText(ParserPath);
        Assert.Contains("validate-production-promotion", parser, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PromotionJsonHasDisabledFlags()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(PromotionJsonPath));
        var root = document.RootElement;

        Assert.False(root.GetProperty("productionApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("stagingApplyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());
    }

    [Fact]
    public void DecisionArtifactPatternsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);
        Assert.Contains("ownership-backfill-production-promotion-decision-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-production-promotion-decision-*.md", gitignore, StringComparison.Ordinal);
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
    public void CrossDocsReferenceProductionPromotionReadiness()
    {
        var proposal = File.ReadAllText(ProposalDocPath);
        var stagingPostRun = File.ReadAllText(StagingPostRunDocPath);

        Assert.Contains("ownership-backfill-production-promotion-readiness.md", proposal, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-production-promotion-readiness.md", stagingPostRun, StringComparison.Ordinal);
    }

    [Fact]
    public void InventoryAndGuardrailsContainP6_13()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var inventoryItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P6-13", inventoryItems);

        using var guardrails = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardrailsIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-PRODUCTION-PROMOTION-READINESS", guardrailsIds);
    }

    [Fact]
    public void DocsDoNotClaimExecutionOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(PromotionDocPath),
            File.ReadAllText(InventoryMarkdownPath),
            File.ReadAllText(ProposalDocPath)
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

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string ParserPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCommandLineParser.cs");

    private static string PromotionDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-promotion-readiness.md");

    private static string PromotionJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-promotion-readiness.json");

    private static string PromotionSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-promotion-readiness.schema.json");

    private static string ProposalDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-production-apply-enablement-proposal.md");

    private static string StagingPostRunDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-staging-post-run-evidence.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
