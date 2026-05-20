using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7SecurityAdrTests
{
    [Fact]
    public void AdrArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            AdrRecordPath,
            AdrIndexMarkdownPath,
            AdrIndexJsonPath,
            AdrIndexSchemaPath,
            DecisionMatrixMarkdownPath,
            DecisionMatrixJsonPath,
            FutureBacklogMarkdownPath,
            FutureBacklogJsonPath);
    }

    [Fact]
    public void AdrContainsRequiredSectionsAndDisabledBoundaryStatements()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            AdrRecordPath,
            [
                "## Status",
                "## Context",
                "## Decision",
                "## Non-goals",
                "## Consequences",
                "## Alternatives considered",
                "## Explicit non-claims",
                "## Follow-up decisions"
            ]);

        var content = File.ReadAllText(AdrRecordPath);
        Assert.Contains("Accepted", content, StringComparison.Ordinal);
        Assert.Contains("write-path remains intentionally disabled", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Staging and production apply are not enabled", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Global EF query filters", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DB row-level security", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Full tenant isolation is not claimed", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdrIndexReferencesAdr0001AndIncludesNonClaims()
    {
        using var index = GovernanceJsonTestHelper.Parse(AdrIndexJsonPath);
        var records = index.RootElement.GetProperty("records").EnumerateArray().ToArray();
        Assert.NotEmpty(records);

        var adr0001 = records.Single(record =>
            string.Equals(record.GetProperty("id").GetString(), "ADR-0001", StringComparison.Ordinal));

        Assert.Equal("Accepted", adr0001.GetProperty("status").GetString());
        Assert.Equal("docs/adr/ADR-0001-security-governance-boundary.md", adr0001.GetProperty("path").GetString());
        Assert.NotEmpty(adr0001.GetProperty("nonClaims").EnumerateArray());

        var matrices = index.RootElement.GetProperty("decisionMatrices").EnumerateArray().ToArray();
        Assert.Contains(matrices, matrix =>
            string.Equals(matrix.GetProperty("markdownPath").GetString(), "docs/adr/security-architecture-decision-matrix.md", StringComparison.Ordinal));

        var backlogs = index.RootElement.GetProperty("companionBacklogs").EnumerateArray().ToArray();
        Assert.Contains(backlogs, backlog =>
            string.Equals(backlog.GetProperty("markdownPath").GetString(), "docs/adr/future-security-adr-backlog.md", StringComparison.Ordinal));
    }

    private static string AdrRecordPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "ADR-0001-security-governance-boundary.md");

    private static string AdrIndexMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "adr-index.md");

    private static string AdrIndexJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "adr-index.json");

    private static string AdrIndexSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "adr-index.schema.json");

    private static string DecisionMatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "security-architecture-decision-matrix.md");

    private static string DecisionMatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "security-architecture-decision-matrix.json");

    private static string FutureBacklogMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "future-security-adr-backlog.md");

    private static string FutureBacklogJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "future-security-adr-backlog.json");
}
