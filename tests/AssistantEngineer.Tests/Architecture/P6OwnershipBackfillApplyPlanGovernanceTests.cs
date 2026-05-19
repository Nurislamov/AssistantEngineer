using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6OwnershipBackfillApplyPlanGovernanceTests
{
    [Fact]
    public void PlanApplyArtifactsExist()
    {
        Assert.True(File.Exists(PlanGeneratorDocPath), $"Missing plan generator doc: {PlanGeneratorDocPath}");
        Assert.True(File.Exists(PlanGeneratorPath), $"Missing plan generator source: {PlanGeneratorPath}");
        Assert.True(File.Exists(PlanWriterPath), $"Missing plan writer source: {PlanWriterPath}");
    }

    [Fact]
    public void PlanApplyCommandExistsAndApplyRemainsDisabled()
    {
        var parser = File.ReadAllText(ParserPath);
        var cli = File.ReadAllText(CliPath);

        Assert.Contains("plan-apply", parser, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Apply mode is designed but disabled", cli, StringComparison.OrdinalIgnoreCase);
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
    public void PlanArtifactsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);

        Assert.Contains("artifacts/ownership-backfill/", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-apply-plan-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-apply-summary-draft-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-apply-summary-draft-*.md", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-planned-records-*.json", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyDesignReferencesPlanHashRequirement()
    {
        var applyDesign = File.ReadAllText(ApplyDesignDocPath);
        Assert.Contains("PlanHash", applyDesign, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionInventoryContainsP6_05()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-05", items);
    }

    [Fact]
    public void GuardrailsContainP6_05PlanOnlyGuard()
    {
        var markdown = File.ReadAllText(GuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-ONLY", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardIds = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-ONLY", guardIds);
    }

    [Fact]
    public void DocsDoNotClaimApplyExecutedOrBackfillCompleteOrGlobalFiltersOrRlsOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(PlanGeneratorDocPath),
            File.ReadAllText(ApplyDesignDocPath),
            File.ReadAllText(StrategyDocPath),
            File.ReadAllText(InventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("apply mode is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill is fully complete", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string ParserPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCommandLineParser.cs");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");

    private static string PlanGeneratorPath =>
        Path.Combine(ToolDirectoryPath, "Plan", "OwnershipBackfillApplyPlanGenerator.cs");

    private static string PlanWriterPath =>
        Path.Combine(ToolDirectoryPath, "Plan", "OwnershipBackfillApplyPlanWriter.cs");

    private static string PlanGeneratorDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-plan-generator.md");

    private static string ApplyDesignDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.md");

    private static string StrategyDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
