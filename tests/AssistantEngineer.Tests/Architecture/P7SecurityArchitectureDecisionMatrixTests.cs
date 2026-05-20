using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7SecurityArchitectureDecisionMatrixTests
{
    [Fact]
    public void DecisionMatrixArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            MatrixMarkdownPath,
            MatrixJsonPath,
            MatrixSchemaPath);
    }

    [Fact]
    public void DecisionMatrixDocumentContainsRequiredSectionsAndNonClaims()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            MatrixMarkdownPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## Decision categories",
                "## Accepted decisions",
                "## Deferred decisions",
                "## Rejected alternatives",
                "## Future ADR-required decisions",
                "## Cross-links to security docs",
                "## Release boundary relationship",
                "## Known limitations",
                "## Next steps"
            ]);

        GovernanceDocumentTestHelper.AssertMarkdownContainsPhrases(
            MatrixMarkdownPath,
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
    public void DecisionMatrixJsonParsesAndReleaseBoundaryFlagsRemainFalse()
    {
        using var matrix = GovernanceJsonTestHelper.Parse(MatrixJsonPath);
        var root = matrix.RootElement;

        GovernanceAssertions.AssertReleaseBoundaryDisabled(
            root.GetProperty("releaseBoundary"),
            [
                "productionApplyEnabled",
                "stagingApplyEnabled",
                "ownershipBackfillExecuted",
                "globalEfQueryFiltersEnabled",
                "databaseRowLevelSecurityEnabled",
                "fullTenantIsolationClaimed"
            ]);

        var acceptedTitles = root.GetProperty("acceptedDecisions")
            .EnumerateArray()
            .Select(item => item.GetProperty("title").GetString() ?? string.Empty)
            .ToArray();
        Assert.Contains(acceptedTitles, title => title.Contains("options-controlled", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(acceptedTitles, title => title.Contains("write-path remains disabled", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(acceptedTitles, title => title.Contains("query filters", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(acceptedTitles, title => title.Contains("row-level security", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(acceptedTitles, title => title.Contains("not claimed", StringComparison.OrdinalIgnoreCase));

        var futureDecisionIds = root.GetProperty("futureAdrRequiredDecisions")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("ADR-FUTURE-001", futureDecisionIds);
        Assert.Contains("ADR-FUTURE-002", futureDecisionIds);
        Assert.Contains("ADR-FUTURE-003", futureDecisionIds);
        Assert.Contains("ADR-FUTURE-004", futureDecisionIds);

        Assert.NotEmpty(root.GetProperty("nonClaims").EnumerateArray());
    }

    [Fact]
    public void Adr0001AndSecurityReleaseBoundaryReferenceDecisionMatrix()
    {
        GovernanceDocumentTestHelper.AssertMarkdownReferences(
            Adr0001Path,
            [
                "security-architecture-decision-matrix.md"
            ]);

        GovernanceDocumentTestHelper.AssertMarkdownReferences(
            SecurityReleaseBoundaryPath,
            [
                "security-architecture-decision-matrix.md"
            ]);
    }

    private static string MatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "security-architecture-decision-matrix.md");

    private static string MatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "security-architecture-decision-matrix.json");

    private static string MatrixSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "security-architecture-decision-matrix.schema.json");

    private static string Adr0001Path =>
        Path.Combine(TestPaths.RepoRoot, "docs", "adr", "ADR-0001-security-governance-boundary.md");

    private static string SecurityReleaseBoundaryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-release-boundary.md");
}
