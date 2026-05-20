using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7SecurityGovernanceStatusVocabularyTests
{
    private static readonly string[] RequiredStatuses =
    [
        "Implemented",
        "AuditOnly",
        "DesignOnly",
        "StrategyOnly",
        "GovernanceOnly",
        "ToolingOnly",
        "TestOnly",
        "DisabledBoundary",
        "Reference",
        "Template",
        "NeedsCleanup",
        "Superseded"
    ];

    [Fact]
    public void StatusVocabularyArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            StatusVocabularyMarkdownPath,
            StatusVocabularyJsonPath);
    }

    [Fact]
    public void StatusVocabularyContainsRequiredStatuses()
    {
        using var vocabularyDoc = GovernanceJsonTestHelper.Parse(StatusVocabularyJsonPath);
        var statuses = vocabularyDoc.RootElement.GetProperty("statuses")
            .EnumerateArray()
            .Select(item => item.GetProperty("status").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var requiredStatus in RequiredStatuses)
            Assert.Contains(requiredStatus, statuses);
    }

    [Fact]
    public void InventoryAndIndexUseKnownStatusesFromVocabulary()
    {
        using var vocabularyDoc = GovernanceJsonTestHelper.Parse(StatusVocabularyJsonPath);
        var statuses = vocabularyDoc.RootElement.GetProperty("statuses")
            .EnumerateArray()
            .Select(item => item.GetProperty("status").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        using var inventoryDoc = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var inventoryStatuses = inventoryDoc.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("status").GetString() ?? string.Empty)
            .ToArray();
        Assert.NotEmpty(inventoryStatuses);

        foreach (var status in inventoryStatuses)
            Assert.Contains(status, statuses);

        using var indexDoc = GovernanceJsonTestHelper.Parse(IndexJsonPath);
        var indexStatuses = indexDoc.RootElement.GetProperty("documents")
            .EnumerateArray()
            .Select(item => item.GetProperty("status").GetString() ?? string.Empty)
            .ToArray();
        Assert.NotEmpty(indexStatuses);

        foreach (var status in indexStatuses)
            Assert.Contains(status, statuses);
    }

    private static string StatusVocabularyMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-status-vocabulary.md");

    private static string StatusVocabularyJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-status-vocabulary.json");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string IndexJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.json");
}
