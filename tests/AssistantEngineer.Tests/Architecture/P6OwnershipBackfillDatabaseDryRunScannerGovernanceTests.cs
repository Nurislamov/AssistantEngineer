using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6OwnershipBackfillDatabaseDryRunScannerGovernanceTests
{
    [Fact]
    public void DatabaseScannerFileExists()
    {
        Assert.True(File.Exists(DatabaseScannerPath), $"Missing scanner file: {DatabaseScannerPath}");
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
    public void DocsReferenceDatabaseDryRunScannerAndP6_02()
    {
        var dryRunTool = File.ReadAllText(DryRunToolDocPath);
        var strategy = File.ReadAllText(StrategyDocPath);

        Assert.Contains("database dry-run", dryRunTool, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("read-only", dryRunTool, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("P6-02", strategy, StringComparison.Ordinal);
    }

    [Fact]
    public void InventoryContainsP6_02RoadmapItem()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-02", roadmapItems);
    }

    [Fact]
    public void DocsDoNotClaimBackfillExecutionOrFullIsolationOrGlobalFiltersOrRls()
    {
        var docs = new[]
        {
            File.ReadAllText(DryRunToolDocPath),
            File.ReadAllText(DatabaseScannerDocPath),
            File.ReadAllText(StrategyDocPath),
            File.ReadAllText(InventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("ownership backfill has been executed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string DatabaseScannerPath =>
        Path.Combine(ToolDirectoryPath, "Scanning", "DatabaseOwnershipBackfillDryRunScanner.cs");

    private static string DryRunToolDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-dry-run-tool.md");

    private static string DatabaseScannerDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-database-dry-run-scanner.md");

    private static string StrategyDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");
}
