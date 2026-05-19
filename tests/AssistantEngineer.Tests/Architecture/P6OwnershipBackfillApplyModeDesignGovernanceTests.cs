using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6OwnershipBackfillApplyModeDesignGovernanceTests
{
    [Fact]
    public void ApplyModeDesignArtifactsExist()
    {
        Assert.True(File.Exists(ApplyDesignMarkdownPath), $"Missing apply-mode design doc: {ApplyDesignMarkdownPath}");
        Assert.True(File.Exists(ApplyDesignJsonPath), $"Missing apply-mode design JSON: {ApplyDesignJsonPath}");
        Assert.True(File.Exists(ApplyDesignSchemaPath), $"Missing apply-mode design schema: {ApplyDesignSchemaPath}");
    }

    [Fact]
    public void ApplyModeDesignDocContainsRequiredSections()
    {
        var content = File.ReadAllText(ApplyDesignMarkdownPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Apply preconditions",
            "## Confirmation phrase",
            "## Apply command contract",
            "## Batch policy",
            "## Previous-values snapshot",
            "## Rollback design",
            "## Idempotency model",
            "## Audit/observability",
            "## Disabled status"
        };

        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyModeDesignDocContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ApplyDesignMarkdownPath);
        var requiredPhrases = new[]
        {
            "No ownership backfill execution claim",
            "No apply mode enabled claim",
            "No full multi-tenant isolation claim yet",
            "No database row-level security claim",
            "No global EF query filter claim",
            "No production security certification claim",
            "No certified/certification claim"
        };

        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyModeDesignJsonIsDisabledAndHasExactConfirmationPhrase()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ApplyDesignJsonPath));
        var root = document.RootElement;

        Assert.Equal("P6-04", root.GetProperty("stage").GetString());
        Assert.False(root.GetProperty("applyEnabled").GetBoolean());
        Assert.False(root.GetProperty("backfillExecution").GetBoolean());
        Assert.Equal(
            "I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA",
            root.GetProperty("confirmationPhrase").GetString());
    }

    [Fact]
    public void ToolApplyCommandExistsButRemainsDisabled()
    {
        var parserContent = File.ReadAllText(ParserPath);
        var cliContent = File.ReadAllText(CliPath);

        Assert.Contains("apply", parserContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("designed but disabled", cliContent, StringComparison.OrdinalIgnoreCase);
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
    public void ProductionInventoryContainsP6_04()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var items = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-04", items);
    }

    [Fact]
    public void DocsDoNotClaimApplyExecutedOrBackfillCompleteOrFullIsolationOrFiltersOrRls()
    {
        var docs = new[]
        {
            File.ReadAllText(ApplyDesignMarkdownPath),
            File.ReadAllText(StrategyMarkdownPath),
            File.ReadAllText(InventoryMarkdownPath),
            File.ReadAllText(DryRunToolMarkdownPath)
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

    private static string ApplyDesignMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.md");

    private static string ApplyDesignJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.json");

    private static string ApplyDesignSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-apply-mode-design.schema.json");

    private static string StrategyMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.md");

    private static string DryRunToolMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-dry-run-tool.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");
}
