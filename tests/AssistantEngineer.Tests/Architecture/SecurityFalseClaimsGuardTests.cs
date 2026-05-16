namespace AssistantEngineer.Tests.Architecture;

public sealed class SecurityFalseClaimsGuardTests
{
    [Fact]
    public void SecurityAndFrontendDocsDoNotContainForbiddenPositiveSecurityClaims()
    {
        var forbiddenPhrases = new[]
        {
            "SOC 2 compliant",
            "ISO 27001 compliant",
            "production certified",
            "certified security",
            "full tenant isolation",
            "complete multi-tenant isolation",
            "all endpoints protected",
            "fully secure",
            "bulletproof security"
        };

        var markdownFiles = Directory
            .EnumerateFiles(SecurityDocsRootPath, "*.md", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(FrontendDocsRootPath, "*.md", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var violations = new List<string>();

        foreach (var file in markdownFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                foreach (var phrase in forbiddenPhrases)
                {
                    if (!line.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (IsAllowedContext(line))
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
                    violations.Add($"{relativePath}:{i + 1} -> {phrase}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Forbidden positive security/compliance claims detected:\n" + string.Join('\n', violations));
    }

    private static bool IsAllowedContext(string line)
    {
        return line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Non-claims", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Future", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Roadmap", StringComparison.OrdinalIgnoreCase);
    }

    private static string SecurityDocsRootPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security");

    private static string FrontendDocsRootPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "frontend");
}
