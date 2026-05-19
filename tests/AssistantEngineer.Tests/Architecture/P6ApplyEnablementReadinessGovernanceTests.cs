using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6ApplyEnablementReadinessGovernanceTests
{
    [Fact]
    public void ValidateApplyReadinessCommandExists()
    {
        var parser = File.ReadAllText(ParserPath);
        Assert.Contains("validate-apply-readiness", parser, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyCommandRemainsDisabled()
    {
        var cli = File.ReadAllText(CliPath);
        Assert.Contains("Apply mode is designed but disabled", cli, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadinessDocsExist()
    {
        Assert.True(File.Exists(ReadinessDocPath), $"Missing readiness doc: {ReadinessDocPath}");
    }

    [Fact]
    public void ReadinessArtifactsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);
        Assert.Contains("ownership-backfill-apply-readiness-result-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-apply-readiness-result-*.md", gitignore, StringComparison.Ordinal);
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
    public void ApplyDesignAndSignoffDocsReferenceReadinessGate()
    {
        var applyDesign = File.ReadAllText(ApplyDesignDocPath);
        var signoffDoc = File.ReadAllText(SignoffDocPath);

        Assert.Contains("apply-readiness", applyDesign, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("readiness gate", signoffDoc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionInventoryContainsP6_08()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-08", items);
    }

    [Fact]
    public void GuardrailsContainApplyReadinessGateGuard()
    {
        var markdown = File.ReadAllText(GuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-READINESS", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardIds = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-READINESS", guardIds);
    }

    [Fact]
    public void DocsDoNotClaimApplyEnabledOrBackfillCompleteOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(ReadinessDocPath),
            File.ReadAllText(ApplyDesignDocPath),
            File.ReadAllText(InventoryMarkdownPath)
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

    private static string ParserPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCommandLineParser.cs");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");

    private static string ReadinessDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-enablement-readiness.md");

    private static string ApplyDesignDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.md");

    private static string SignoffDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-plan-signoff-gate.md");

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

