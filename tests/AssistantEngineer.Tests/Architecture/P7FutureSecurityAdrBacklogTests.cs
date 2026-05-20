using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7FutureSecurityAdrBacklogTests
{
    [Fact]
    public void FutureAdrBacklogArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            BacklogMarkdownPath,
            BacklogJsonPath,
            BacklogSchemaPath);
    }

    [Fact]
    public void FutureAdrBacklogDocumentContainsRequiredSectionsAndNonClaims()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            BacklogMarkdownPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## ADR-required future decisions",
                "## Trigger conditions",
                "## Required evidence before ADR",
                "## Forbidden shortcut decisions",
                "## Relationship to P7/P8 roadmap"
            ]);

        GovernanceDocumentTestHelper.AssertMarkdownContainsPhrases(
            BacklogMarkdownPath,
            [
                "No production apply enabled claim.",
                "No staging apply execution claim.",
                "No ownership backfill execution claim.",
                "No global EF query filter claim.",
                "No database row-level security claim.",
                "No full multi-tenant isolation claim yet.",
                "No production security certification claim."
            ]);
    }

    [Fact]
    public void FutureAdrBacklogJsonParsesAndContainsRequiredItemsAndFields()
    {
        using var backlog = GovernanceJsonTestHelper.Parse(BacklogJsonPath);
        var root = backlog.RootElement;

        var requiredIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "ADR-FUTURE-001",
            "ADR-FUTURE-002",
            "ADR-FUTURE-003",
            "ADR-FUTURE-004",
            "ADR-FUTURE-005",
            "ADR-FUTURE-006",
            "ADR-FUTURE-007",
            "ADR-FUTURE-008"
        };

        foreach (var item in root.GetProperty("backlogItems").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString() ?? string.Empty;
            requiredIds.Remove(id);

            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("triggerCondition").GetString()));
            Assert.NotEmpty(item.GetProperty("requiredEvidence").EnumerateArray());
            Assert.NotEmpty(item.GetProperty("forbiddenShortcuts").EnumerateArray());

            var serialized = item.GetRawText();
            Assert.DoesNotContain("apply enabled", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("backfill executed", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation complete", serialized, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Empty(requiredIds);
    }

    private static string BacklogMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "future-security-adr-backlog.md");

    private static string BacklogJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "future-security-adr-backlog.json");

    private static string BacklogSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "future-security-adr-backlog.schema.json");
}
