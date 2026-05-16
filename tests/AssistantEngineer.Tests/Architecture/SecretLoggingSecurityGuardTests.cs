using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture;

public sealed partial class SecretLoggingSecurityGuardTests
{
    [Fact]
    public void HighConfidenceSecretLoggingPatternsAreNotPresent()
    {
        var violations = new List<string>();
        var allowlist = ParseAllowlistEntries(AllowlistPath);

        var sourceRoots = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "src", "Backend"),
            Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "src")
        };

        foreach (var sourceRoot in sourceRoots)
        {
            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories))
            {
                if (!IsSupportedSourceFile(file))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
                if (relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                    relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);
                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    if (!ContainsHighConfidenceSecretLoggingPattern(line))
                    {
                        continue;
                    }

                    var lineNumber = index + 1;
                    if (IsAllowlisted(allowlist, relative, lineNumber))
                    {
                        continue;
                    }

                    violations.Add($"{relative}:{lineNumber}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "High-confidence secret logging/source patterns detected:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void AppsettingsDoNotContainRealLookingSecretValues()
    {
        var files = new[]
        {
            Path.Combine(TestPaths.ApiProjectPath, "appsettings.json"),
            Path.Combine(TestPaths.ApiProjectPath, "appsettings.Development.json")
        };

        var violations = new List<string>();

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("\"ApiKeyHeaderName\"", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("\"Authorization\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (SecretValuePattern().IsMatch(line))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)}:{i + 1}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Potential hardcoded secret values found in appsettings:\n" + string.Join('\n', violations));
    }

    private static bool IsSupportedSourceFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension is ".cs" or ".ts" or ".tsx" or ".js" or ".jsx";
    }

    private static bool ContainsHighConfidenceSecretLoggingPattern(string line)
    {
        if (line.Contains("AuditMetadataSanitizer", StringComparison.Ordinal))
        {
            return false;
        }

        var loggerLike = LoggerCallPattern().IsMatch(line) || ConsoleWritePattern().IsMatch(line);
        if (!loggerLike)
        {
            return false;
        }

        if (SensitivePlaceholderPattern().IsMatch(line))
        {
            return true;
        }

        if (SensitiveInterpolatedVariablePattern().IsMatch(line))
        {
            return true;
        }

        return false;
    }

    private static bool IsAllowlisted(HashSet<string> allowlist, string relativePath, int lineNumber)
    {
        return allowlist.Contains(relativePath) ||
               allowlist.Contains($"{relativePath}:{lineNumber}");
    }

    private static HashSet<string> ParseAllowlistEntries(string path)
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

    [GeneratedRegex(@"\b(LogTrace|LogDebug|LogInformation|LogWarning|LogError|LogCritical)\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex LoggerCallPattern();

    [GeneratedRegex(@"Console\.Write(Line)?\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex ConsoleWritePattern();

    [GeneratedRegex(@"\{[^}]*?(api[_-]?key|token|password|secret|authorization|cookie)[^}]*\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitivePlaceholderPattern();

    [GeneratedRegex(@"\$""[^""]*(api[_-]?key|token|password|secret|authorization|cookie)[^""]*""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveInterpolatedVariablePattern();

    [GeneratedRegex(@"""(ApiKey|Token|Password|Secret|Authorization|Cookie|Key)""\s*:\s*""[^""]{8,}""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretValuePattern();

    private static string AllowlistPath =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "security", "secret-logging-source-allowlist.txt");
}
