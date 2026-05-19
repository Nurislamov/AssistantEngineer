using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6TestOnlyApplyExecutorGovernanceTests
{
    [Fact]
    public void TestOnlyExecutorExistsAndCliApplyRemainsDisabled()
    {
        Assert.True(File.Exists(TestOnlyExecutorPath), $"Missing test-only executor: {TestOnlyExecutorPath}");

        var cli = File.ReadAllText(CliPath);
        Assert.Contains("Apply mode is designed but disabled", cli, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CliIsNotWiredToRealApplyExecutor()
    {
        var program = File.ReadAllText(ProgramPath);
        var cli = File.ReadAllText(CliPath);

        Assert.DoesNotContain("IOwnershipBackfillApplyExecutor", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IOwnershipBackfillApplyExecutor", cli, StringComparison.Ordinal);
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
            Assert.False(Regex.IsMatch(content, @"\bUPDATE\s+\w+\s+SET\b", RegexOptions.IgnoreCase), $"Detected SQL UPDATE pattern in {file}");
            Assert.False(Regex.IsMatch(content, @"\bDELETE\s+FROM\b", RegexOptions.IgnoreCase), $"Detected SQL DELETE pattern in {file}");
            Assert.False(Regex.IsMatch(content, @"\bTRUNCATE\s+TABLE\b", RegexOptions.IgnoreCase), $"Detected SQL TRUNCATE pattern in {file}");
            Assert.False(Regex.IsMatch(content, @"\bINSERT\s+INTO\b", RegexOptions.IgnoreCase), $"Detected SQL INSERT pattern in {file}");
        }
    }

    [Fact]
    public void RehearsalArtifactsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);
        Assert.Contains("ownership-backfill-apply-rehearsal-result-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-apply-rehearsal-result-*.md", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-rehearsal-previous-values-*.json", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void DocsStateTestOnlyAndNoBackfillExecutionClaims()
    {
        var rehearsalDoc = File.ReadAllText(RehearsalDocPath);
        var applyDesignDoc = File.ReadAllText(ApplyDesignDocPath);

        Assert.Contains("test-only", rehearsalDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not enable apply", applyDesignDoc, StringComparison.OrdinalIgnoreCase);

        var docs = new[]
        {
            rehearsalDoc,
            applyDesignDoc,
            File.ReadAllText(InventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill is fully complete", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ProductionInventoryContainsP6_07()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-07", items);
    }

    [Fact]
    public void GuardrailsContainTestOnlyApplyGuard()
    {
        var markdown = File.ReadAllText(GuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-TEST-ONLY-APPLY-REHEARSAL", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardIds = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-TEST-ONLY-APPLY-REHEARSAL", guardIds);
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string TestOnlyExecutorPath =>
        Path.Combine(ToolDirectoryPath, "Apply", "TestOnlyOwnershipBackfillApplyExecutor.cs");

    private static string ProgramPath =>
        Path.Combine(ToolDirectoryPath, "Program.cs");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");

    private static string RehearsalDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-test-only-apply-rehearsal.md");

    private static string ApplyDesignDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.md");

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
