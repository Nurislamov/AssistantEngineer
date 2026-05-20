using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture.Governance;

internal static class GovernanceAssertions
{
    public static void AssertRequiredNonClaims(string content, IReadOnlyList<string> requiredPhrases)
    {
        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertReleaseBoundaryDisabled(JsonElement releaseBoundaryElement, IReadOnlyList<string> falseFlags)
    {
        GovernanceJsonTestHelper.AssertBooleanPropertiesFalse(releaseBoundaryElement, falseFlags);
    }

    public static void AssertNoFalseSecurityClaims(
        IReadOnlyList<string> files,
        IReadOnlyList<string> forbiddenPhrases)
    {
        var violations = GovernanceClaimTestHelper.FindForbiddenPhraseViolations(files, forbiddenPhrases);
        Assert.True(violations.Count == 0,
            "Forbidden positive claims found:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    public static void AssertGeneratedArtifactsIgnored(string gitignorePath, IReadOnlyList<string> requiredPatterns)
    {
        var gitignore = File.ReadAllText(gitignorePath);
        foreach (var pattern in requiredPatterns)
            Assert.Contains(pattern, gitignore, StringComparison.Ordinal);
    }
}
