using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7SecurityReleaseBoundaryTests
{
    [Fact]
    public void ReleaseBoundaryArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            ReleaseBoundaryDocPath,
            ReleaseBoundaryJsonPath,
            ReleaseBoundarySchemaPath);
    }

    [Fact]
    public void ReleaseBoundaryDocContainsRequiredSectionsAndNonClaims()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            ReleaseBoundaryDocPath,
            [
                "## Purpose",
                "## Scope",
                "## Current boundary",
                "## Enabled capabilities",
                "## Intentionally disabled capabilities",
                "## Allowed claims",
                "## Forbidden claims",
                "## Runtime behavior boundary",
                "## Backfill/write-path boundary",
                "## Tenant isolation boundary",
                "## Release verification boundary",
                "## Relationship to P5/P6/P7 docs",
                "## Non-claims"
            ]);

        var content = File.ReadAllText(ReleaseBoundaryDocPath);
        GovernanceAssertions.AssertRequiredNonClaims(
            content,
            [
                "No production apply enabled claim.",
                "No ownership backfill execution claim.",
                "No global EF query filter claim.",
                "No database row-level security claim.",
                "No full multi-tenant isolation claim yet."
            ]);
    }

    [Fact]
    public void ReleaseBoundaryJsonParsesAndAllDisabledFlagsAreFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReleaseBoundaryJsonPath);
        var root = document.RootElement;
        var flags = root.GetProperty("releaseBoundary");

        GovernanceAssertions.AssertReleaseBoundaryDisabled(
            flags,
            [
                "productionApplyEnabled",
                "stagingApplyEnabled",
                "ownershipBackfillExecuted",
                "dbWritePathEnabled",
                "globalEfQueryFiltersEnabled",
                "databaseRowLevelSecurityEnabled",
                "fullTenantIsolationClaimed",
                "productionSecurityCertified"
            ]);
    }

    [Fact]
    public void ReleaseBoundaryJsonContainsForbiddenClaimsList()
    {
        using var document = GovernanceJsonTestHelper.Parse(ReleaseBoundaryJsonPath);
        var forbidden = document.RootElement.GetProperty("forbiddenClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(forbidden, item => item.Contains("Production apply enabled", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, item => item.Contains("Ownership backfill executed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, item => item.Contains("Database row-level security enabled", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, item => item.Contains("Global EF query filters enabled", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReleaseBoundaryDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-release-boundary.md");

    private static string ReleaseBoundaryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-release-boundary.json");

    private static string ReleaseBoundarySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-release-boundary.schema.json");
}
