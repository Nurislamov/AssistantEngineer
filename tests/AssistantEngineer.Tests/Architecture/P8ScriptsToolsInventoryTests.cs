using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8ScriptsToolsInventoryTests
{
    [Fact]
    public void ScriptsToolsInventoryArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            InventoryMarkdownPath,
            InventoryJsonPath,
            InventorySchemaPath);
    }

    [Fact]
    public void ScriptsToolsInventoryUsesKnownCategoriesAndCriticalClassifications()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "KeepAsWrapper",
            "ConvertToToolCandidate",
            "DeprecatedCandidate",
            "ReleaseGateCritical",
            "GeneratedArtifactProducer",
            "UnknownNeedsReview"
        };

        var entries = root.GetProperty("entries").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);

        foreach (var entry in entries)
        {
            var category = entry.GetProperty("category").GetString() ?? string.Empty;
            Assert.Contains(category, allowed);

            var path = entry.GetProperty("path").GetString() ?? string.Empty;
            Assert.DoesNotContain("deleted", path, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains(entries, entry =>
            string.Equals(entry.GetProperty("path").GetString(), "scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1", StringComparison.Ordinal) &&
            string.Equals(entry.GetProperty("category").GetString(), "ReleaseGateCritical", StringComparison.Ordinal));

        Assert.Contains(entries, entry =>
            string.Equals(entry.GetProperty("path").GetString(), "tools/AssistantEngineer.Tools.OwnershipBackfill", StringComparison.Ordinal) &&
            string.Equals(entry.GetProperty("category").GetString(), "ReleaseGateCritical", StringComparison.Ordinal));
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.schema.json");
}
