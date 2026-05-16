using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture;

public sealed class FrontendSecretsGuardTests
{
    [Fact]
    public void FrontendSourceDoesNotContainHighConfidenceHardcodedSecretsOrTokenStoragePatterns()
    {
        var allowlist = ParseAllowlist(AllowlistPath);
        var sourceFiles = Directory.EnumerateFiles(FrontendSourceRootPath, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(".test.", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(".spec.", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\__tests__\\", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in sourceFiles)
        {
            var relative = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
            if (allowlist.Contains(relative))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!ContainsHighConfidenceViolation(line))
                {
                    continue;
                }

                var lineEntry = $"{relative}:{i + 1}";
                if (allowlist.Contains(lineEntry))
                {
                    continue;
                }

                violations.Add(lineEntry);
            }
        }

        Assert.True(
            violations.Count == 0,
            "Potential frontend hardcoded token/key patterns detected:\n" + string.Join('\n', violations));
    }

    private static bool ContainsHighConfidenceViolation(string line)
    {
        if (BearerTokenLiteralPattern.IsMatch(line))
        {
            return true;
        }

        if (ApiKeyLiteralPattern.IsMatch(line) || TokenLiteralPattern.IsMatch(line))
        {
            return true;
        }

        if (ApiKeyEnvLiteralPattern.IsMatch(line))
        {
            return true;
        }

        if (LocalStorageTokenPattern.IsMatch(line) || LocalStorageApiKeyPattern.IsMatch(line))
        {
            return true;
        }

        return false;
    }

    private static HashSet<string> ParseAllowlist(string path)
    {
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static readonly Regex BearerTokenLiteralPattern =
        new(@"Bearer\s+[A-Za-z0-9\-_\.]{16,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ApiKeyLiteralPattern =
        new(@"\bapiKey\s*:\s*['""][^'""]{8,}['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TokenLiteralPattern =
        new(@"\btoken\s*:\s*['""][^'""]{8,}['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ApiKeyEnvLiteralPattern =
        new(@"API_KEY\s*=\s*['""][^'""]{8,}['""]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LocalStorageTokenPattern =
        new(@"localStorage\.setItem\(\s*['""]token['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex LocalStorageApiKeyPattern =
        new(@"localStorage\.setItem\(\s*['""]apiKey['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static string FrontendSourceRootPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "src");

    private static string AllowlistPath =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "security", "frontend-secrets-source-allowlist.txt");
}
