using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture.Governance;

internal static class GovernanceSemanticAssertions
{
    public static void AssertJsonBooleanFlagsFalse(JsonElement root, IReadOnlyList<string> flags)
    {
        foreach (var flag in flags)
        {
            Assert.True(root.TryGetProperty(flag, out var value), $"Missing JSON flag: {flag}");
            Assert.False(value.GetBoolean(), $"Expected '{flag}' to be false.");
        }
    }

    public static void AssertNonClaimsContainConcepts(IEnumerable<string> nonClaims, IReadOnlyList<string> requiredConcepts)
    {
        var items = nonClaims.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        Assert.NotEmpty(items);

        foreach (var concept in requiredConcepts)
        {
            Assert.Contains(
                items,
                item => item.Contains(concept, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void AssertMarkdownContainsDocLinks(string markdownPath, IReadOnlyList<string> requiredDocPaths)
    {
        var content = File.ReadAllText(markdownPath);
        foreach (var requiredDocPath in requiredDocPaths)
        {
            Assert.Contains(requiredDocPath, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void AssertDocumentArtifactsExist(params string[] filePaths)
    {
        GovernanceDocumentTestHelper.AssertFilesExist(filePaths);
    }
}
