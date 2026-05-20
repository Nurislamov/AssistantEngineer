using System.Text;

namespace AssistantEngineer.Tests.Architecture.Governance;

internal static class GovernanceDocumentTestHelper
{
    public static void AssertFilesExist(params string[] paths)
    {
        foreach (var path in paths)
            Assert.True(File.Exists(path), $"Missing file: {path}");
    }

    public static void AssertMarkdownContainsSections(string markdownPath, IReadOnlyList<string> requiredSections)
    {
        var content = File.ReadAllText(markdownPath);
        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);
    }

    public static void AssertMarkdownContainsPhrases(
        string markdownPath,
        IReadOnlyList<string> requiredPhrases,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        var content = File.ReadAllText(markdownPath);
        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, comparison);
    }

    public static void AssertMarkdownReferences(string markdownPath, IReadOnlyList<string> referencedDocNames)
    {
        var content = File.ReadAllText(markdownPath);
        foreach (var docName in referencedDocNames)
            Assert.Contains(docName, content, StringComparison.OrdinalIgnoreCase);
    }

    public static string[] ReadLines(string path)
    {
        return File.ReadAllLines(path, Encoding.UTF8);
    }
}
