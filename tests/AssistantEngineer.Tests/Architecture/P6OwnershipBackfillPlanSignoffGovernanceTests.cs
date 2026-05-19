using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6OwnershipBackfillPlanSignoffGovernanceTests
{
    [Fact]
    public void SignoffPlanCommandExistsAndApplyRemainsDisabled()
    {
        var parser = File.ReadAllText(ParserPath);
        var cli = File.ReadAllText(CliPath);

        Assert.Contains("signoff-plan", parser, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Apply mode is designed but disabled", cli, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SignoffDocsExist()
    {
        Assert.True(File.Exists(SignoffDocPath), $"Missing signoff gate doc: {SignoffDocPath}");
    }

    [Fact]
    public void GeneratedSignoffArtifactsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);

        Assert.Contains("ownership-backfill-plan-signoff-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-plan-signoff-*.md", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolSourceContainsNoSaveChangesAndNoDestructiveSql()
    {
        var files = Directory.GetFiles(ToolDirectoryPath, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        foreach (var file in files)
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
    public void ApplyDesignAndPlanDocsReferenceSignedPlanRequirement()
    {
        var applyDesign = File.ReadAllText(ApplyDesignDocPath);
        var planGenerator = File.ReadAllText(PlanGeneratorDocPath);

        Assert.Contains("signed plan", applyDesign, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("signoff-plan", planGenerator, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionInventoryContainsP6_06()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-06", items);
    }

    [Fact]
    public void GuardrailsContainP6_06SignoffGuard()
    {
        var markdown = File.ReadAllText(GuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-SIGNOFF", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var ids = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-SIGNOFF", ids);
    }

    [Fact]
    public void DocsDoNotClaimApplyExecutedOrBackfillCompleteOrGlobalFiltersOrRlsOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(SignoffDocPath),
            File.ReadAllText(PlanGeneratorDocPath),
            File.ReadAllText(ApplyDesignDocPath),
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

    private static string SignoffDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-plan-signoff-gate.md");

    private static string PlanGeneratorDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-plan-generator.md");

    private static string ApplyDesignDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
