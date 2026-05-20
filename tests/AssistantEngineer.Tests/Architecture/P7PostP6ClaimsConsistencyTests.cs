using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7PostP6ClaimsConsistencyTests
{
    private static readonly string[] ForbiddenPhrases =
    [
        "full tenant isolation complete",
        "production apply enabled",
        "ownership backfill executed",
        "database row-level security enabled",
        "global ef query filters enabled",
        "certified",
        "certification complete",
        "soc 2 compliant",
        "iso 27001 compliant"
    ];

    [Fact]
    public void SecurityDocsDoNotContainForbiddenPositiveClaims()
    {
        var files = Directory.GetFiles(GovernancePathHelper.SecurityDocsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path =>
                path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(files);

        GovernanceAssertions.AssertNoFalseSecurityClaims(files, ForbiddenPhrases);
    }
}
