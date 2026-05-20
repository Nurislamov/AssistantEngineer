using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7GeneratedOwnershipBackfillArtifactsIgnoreTests
{
    [Fact]
    public void GitIgnoreCoversOwnershipBackfillGeneratedArtifacts()
    {
        GovernanceAssertions.AssertGeneratedArtifactsIgnored(
            GitIgnorePath,
            [
                "artifacts/ownership-backfill/",
                "**/ownership-backfill-dry-run-summary*.json",
                "**/ownership-backfill-unresolved-records*.json",
                "**/ownership-backfill-previous-values*.json",
                "**/ownership-backfill-evidence-gate-result-*.json",
                "**/ownership-backfill-apply-plan-*.json",
                "**/ownership-backfill-planned-records-*.json",
                "**/ownership-backfill-plan-signoff-*.json",
                "**/ownership-backfill-apply-readiness-result-*.json",
                "**/ownership-backfill-staging-acceptance-result-*.json",
                "**/ownership-backfill-production-promotion-decision-*.json",
                "**/ownership-backfill-manual-decision-log-*.json",
                "**/ownership-backfill-architecture-review-*.json"
            ]);
    }

    [Fact]
    public void GitIgnoreDoesNotIgnoreSecurityGovernanceJsonAndSchemaDocuments()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);

        Assert.DoesNotContain("docs/security/*.json", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docs/security/*.schema.json", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("**/docs/security/*.json", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("**/docs/security/*.schema.json", gitignore, StringComparison.OrdinalIgnoreCase);
    }

    private static string GitIgnorePath => GovernancePathHelper.GitIgnorePath;
}
