using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8TerminologyClaimsSurfaceCleanupTests
{
    private static readonly string[] ForbiddenPositiveClaims =
    [
        "full pybuildingenergy parity",
        "energyplus parity",
        "ashrae 140 validated",
        "iso certified",
        "full tenant isolation",
        "production security certified",
        "soc2 compliant",
        "iso27001 compliant",
        "production apply enabled",
        "ownership backfill executed",
        "db rls enabled",
        "global ef query filters enabled"
    ];

    [Fact]
    public void CleanupArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            CleanupMarkdownPath,
            CleanupJsonPath,
            CleanupSchemaPath);
    }

    [Fact]
    public void CleanupJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(CleanupJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged"]);

        Assert.NotEmpty(root.GetProperty("claimsCorrected").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("terminologyNormalized").EnumerateArray());
    }

    [Fact]
    public void CleanupReferencesVocabularyAndContainsNonClaims()
    {
        var markdown = File.ReadAllText(CleanupMarkdownPath);
        Assert.Contains("terminology-and-claims-vocabulary", markdown, StringComparison.OrdinalIgnoreCase);

        using var document = GovernanceJsonTestHelper.Parse(CleanupJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            ["No production security certification claim", "No ownership backfill execution claim"]);
    }

    [Fact]
    public void DocsAndClaimTestsDoNotContainForbiddenPositiveClaimsOutsideAllowedContexts()
    {
        var files = Directory
            .EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "docs", "security"), "*.*", SearchOption.TopDirectoryOnly)
            .Where(path =>
                path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Concat(Directory
                .EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "docs", "architecture"), "*.*", SearchOption.TopDirectoryOnly)
                .Where(path =>
                    path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            .Where(path =>
                !path.EndsWith("terminology-and-claims-vocabulary.md", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("terminology-and-claims-vocabulary.json", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("security-release-boundary.md", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith("security-release-boundary.json", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var violations = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lowerLine = line.ToLowerInvariant();

                foreach (var phrase in ForbiddenPositiveClaims)
                {
                    if (!lowerLine.Contains(phrase, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (GovernanceClaimTestHelper.IsAllowedVocabularyOrNonClaimContext(line))
                    {
                        continue;
                    }

                    violations.Add($"{GovernancePathHelper.ToRepoRelative(file)}:{i + 1}: \"{line.Trim()}\"");
                }
            }
        }

        Assert.True(violations.Count == 0, "Forbidden positive claim usage found:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void P8AuditTracksTerminologyClaimsCleanup()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .Single(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F10", StringComparison.Ordinal));

        Assert.Contains(
            finding.GetProperty("resolutionStatus").GetString(),
            new[] { "Addressed", "PartiallyAddressed", "InProgress" });
        Assert.Equal("P8-07", finding.GetProperty("resolutionStage").GetString());
    }

    private static string CleanupMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-claims-surface-cleanup.md");

    private static string CleanupJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-claims-surface-cleanup.json");

    private static string CleanupSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-claims-surface-cleanup.schema.json");
}
