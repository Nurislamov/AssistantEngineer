using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8LegacyAndDeadCodeInventoryTests
{
    [Fact]
    public void LegacyInventoryArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            InventoryMarkdownPath,
            InventoryJsonPath,
            InventorySchemaPath);
    }

    [Fact]
    public void LegacyInventoryJsonUsesKnownCategoriesAndReviewOnlyRecommendations()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "CandidateForReview",
            "CandidateForConsolidation",
            "CandidateForRemovalLater",
            "KeepCritical",
            "UnknownNeedsReview"
        };

        foreach (var entry in root.GetProperty("entries").EnumerateArray())
        {
            var category = entry.GetProperty("category").GetString() ?? string.Empty;
            Assert.Contains(category, allowed);

            var recommendation = entry.GetProperty("recommendedAction").GetString() ?? string.Empty;
            Assert.DoesNotContain("remove now", recommendation, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("deleted", recommendation, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "legacy-and-dead-code-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "legacy-and-dead-code-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "legacy-and-dead-code-inventory.schema.json");
}
