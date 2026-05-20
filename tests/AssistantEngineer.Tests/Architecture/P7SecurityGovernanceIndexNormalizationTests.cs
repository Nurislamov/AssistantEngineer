using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7SecurityGovernanceIndexNormalizationTests
{
    private static readonly string[] RequiredCategories =
    [
        "Canonical release boundary",
        "Route protection",
        "Tenant isolation",
        "Ownership metadata",
        "Ownership backfill toolchain",
        "Apply governance",
        "Staging governance",
        "Production governance",
        "Architecture review",
        "Audit/release readiness"
    ];

    [Fact]
    public void SecurityGovernanceIndexArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            IndexMarkdownPath,
            IndexJsonPath);
    }

    [Fact]
    public void IndexEntriesAreNormalizedAndPointToExistingPaths()
    {
        using var vocabularyDoc = GovernanceJsonTestHelper.Parse(StatusVocabularyJsonPath);
        var knownStatuses = vocabularyDoc.RootElement.GetProperty("statuses")
            .EnumerateArray()
            .Select(item => item.GetProperty("status").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        using var indexDoc = GovernanceJsonTestHelper.Parse(IndexJsonPath);
        var entries = indexDoc.RootElement.GetProperty("documents").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);

        var categories = new HashSet<string>(StringComparer.Ordinal);
        var canonicalRoles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            var path = entry.GetProperty("path").GetString();
            var stage = entry.GetProperty("stage").GetString();
            var category = entry.GetProperty("category").GetString();
            var status = entry.GetProperty("status").GetString();
            var canonicalRole = entry.GetProperty("canonicalRole").GetString();
            var summary = entry.GetProperty("summary").GetString();
            _ = entry.GetProperty("pointsToCanonicalBoundary").GetBoolean();

            Assert.False(string.IsNullOrWhiteSpace(path), "Index path must be non-empty.");
            Assert.False(string.IsNullOrWhiteSpace(stage), "Index stage must be non-empty.");
            Assert.False(string.IsNullOrWhiteSpace(category), "Index category must be non-empty.");
            Assert.False(string.IsNullOrWhiteSpace(status), "Index status must be non-empty.");
            Assert.False(string.IsNullOrWhiteSpace(canonicalRole), "Index canonicalRole must be non-empty.");
            Assert.False(string.IsNullOrWhiteSpace(summary), "Index summary must be non-empty.");

            Assert.Contains(status!, knownStatuses);
            categories.Add(category!);
            canonicalRoles.Add(canonicalRole!);

            var absolutePath = Path.Combine(TestPaths.RepoRoot, path!.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(absolutePath), $"Index entry points to missing file: {path}");
        }

        foreach (var requiredCategory in RequiredCategories)
            Assert.Contains(requiredCategory, categories);

        var allowedRoles = new HashSet<string>(StringComparer.Ordinal)
        {
            "Canonical",
            "StageEvidence",
            "Reference",
            "Template",
            "SupersededBy",
            "NeedsCleanup"
        };

        foreach (var role in canonicalRoles)
            Assert.Contains(role, allowedRoles);
    }

    [Fact]
    public void CanonicalReleaseBoundaryEntryExistsAndIsCanonical()
    {
        using var indexDoc = GovernanceJsonTestHelper.Parse(IndexJsonPath);
        var entries = indexDoc.RootElement.GetProperty("documents").EnumerateArray().ToArray();

        var match = entries.Single(entry =>
            string.Equals(entry.GetProperty("path").GetString(), "docs/security/security-release-boundary.md", StringComparison.Ordinal));

        Assert.Equal("Canonical release boundary", match.GetProperty("category").GetString());
        Assert.Equal("Implemented", match.GetProperty("status").GetString());
        Assert.Equal("Canonical", match.GetProperty("canonicalRole").GetString());
        Assert.True(match.GetProperty("pointsToCanonicalBoundary").GetBoolean());
    }

    [Fact]
    public void KeyP5P6P7DocsAreIndexed()
    {
        using var indexDoc = GovernanceJsonTestHelper.Parse(IndexJsonPath);
        var paths = indexDoc.RootElement.GetProperty("documents")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var requiredPaths =
            new[]
            {
                "docs/security/security-release-boundary.md",
                "docs/security/authorization-policy-rollout.md",
                "docs/security/tenant-isolation-integration-matrix.md",
                "docs/security/workflow-ownership-metadata-coverage.md",
                "docs/security/ownership-backfill-strategy.md",
                "docs/security/ownership-backfill-dry-run-tool.md",
                "docs/security/ownership-backfill-database-dry-run-scanner.md",
                "docs/security/ownership-backfill-evidence-validation-gates.md",
                "docs/security/ownership-backfill-apply-plan-generator.md",
                "docs/security/ownership-backfill-plan-signoff-gate.md",
                "docs/security/ownership-backfill-test-only-apply-rehearsal.md",
                "docs/security/ownership-backfill-apply-enablement-readiness.md",
                "docs/security/ownership-backfill-production-apply-enablement-proposal.md",
                "docs/security/ownership-backfill-staging-apply-runbook.md",
                "docs/security/ownership-backfill-staging-apply-executor-design.md",
                "docs/security/ownership-backfill-staging-post-run-evidence.md",
                "docs/security/ownership-backfill-production-promotion-readiness.md",
                "docs/security/ownership-backfill-manual-write-path-enablement-decision.md",
                "docs/security/ownership-backfill-apply-enablement-architecture-review.md",
                "docs/security/post-p6-governance-audit.md",
                "docs/security/security-governance-status-vocabulary.md",
                "docs/security/governance-test-consolidation-report.md",
                "docs/security/governance-test-consolidation-report.json",
                "docs/security/release-ready-observability-audit.md",
                "docs/security/ci-github-checks-visibility-audit.md",
                "docs/security/ci-github-checks-visibility-runbook.md",
                "docs/security/security-docs-map.md",
                "docs/adr/ADR-0001-security-governance-boundary.md",
                "docs/adr/security-architecture-decision-matrix.md",
                "docs/adr/security-architecture-decision-matrix.json",
                "docs/adr/future-security-adr-backlog.md",
                "docs/adr/future-security-adr-backlog.json",
                "docs/adr/adr-index.md",
                "docs/adr/adr-index.json"
            };

        foreach (var requiredPath in requiredPaths)
            Assert.Contains(requiredPath, paths);
    }

    private static string IndexMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.md");

    private static string IndexJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-index.json");

    private static string StatusVocabularyJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-governance-status-vocabulary.json");
}
