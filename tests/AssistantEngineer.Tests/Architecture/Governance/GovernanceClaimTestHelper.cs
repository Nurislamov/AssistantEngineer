using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture.Governance;

internal static class GovernanceClaimTestHelper
{
    public static IReadOnlyList<string> FindForbiddenPhraseViolations(
        IReadOnlyList<string> files,
        IReadOnlyList<string> forbiddenPhrases)
    {
        var violations = new List<string>();

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                var lowerLine = line.ToLowerInvariant();

                foreach (var phrase in forbiddenPhrases)
                {
                    var searchIndex = 0;
                    while (true)
                    {
                        var matchIndex = lowerLine.IndexOf(phrase, searchIndex, StringComparison.Ordinal);
                        if (matchIndex < 0)
                            break;

                        if (!IsAllowedContext(lines, lineIndex, lowerLine, matchIndex))
                        {
                            var relativePath = GovernancePathHelper.ToRepoRelative(file);
                            violations.Add($"{relativePath}:{lineIndex + 1}: \"{line.Trim()}\"");
                        }

                        searchIndex = matchIndex + phrase.Length;
                    }
                }
            }
        }

        return violations;
    }

    public static bool ContainsForbiddenPositiveClaim(string line, string forbiddenPhrase)
    {
        if (!line.Contains(forbiddenPhrase, StringComparison.OrdinalIgnoreCase))
            return false;

        return !IsAllowedLineOnlyContext(line);
    }

    public static bool IsAllowedVocabularyOrNonClaimContext(string line)
    {
        var lower = line.ToLowerInvariant();
        return IsAllowedLineOnlyContext(line) ||
               lower.Contains("forbidden:", StringComparison.Ordinal) ||
               lower.Contains("deprecated", StringComparison.Ordinal) ||
               lower.Contains("examples", StringComparison.Ordinal) ||
               lower.Contains("known limitations", StringComparison.Ordinal) ||
               lower.Contains("\"forbiddenclaims\"", StringComparison.Ordinal) ||
               lower.Contains("\"deprecatedterms\"", StringComparison.Ordinal) ||
               lower.Contains("\"allowedclaims\"", StringComparison.Ordinal) ||
               lower.Contains("not claimed", StringComparison.Ordinal) ||
               lower.Contains("not enabled", StringComparison.Ordinal) ||
               lower.Contains("deferred", StringComparison.Ordinal) ||
               lower.Contains("eligibility", StringComparison.Ordinal) ||
               lower.Contains("overclaim", StringComparison.Ordinal);
    }

    private static bool IsAllowedContext(string[] lines, int lineIndex, string lowerLine, int matchIndex)
    {
        if (IsAllowedLineOnlyContext(lines[lineIndex]))
            return true;

        if (lowerLine.Contains("productionsecuritycertified", StringComparison.Ordinal))
            return true;

        var localStart = Math.Max(0, lineIndex - 4);
        for (var i = localStart; i <= lineIndex; i++)
        {
            var context = lines[i].ToLowerInvariant();
            if (context.Contains("forbidden claims", StringComparison.Ordinal) ||
                context.Contains("\"forbiddenclaims\"", StringComparison.Ordinal) ||
                context.Contains("\"nonclaims\"", StringComparison.Ordinal) ||
                context.Contains("\"forbiddenclaims\"", StringComparison.Ordinal))
            {
                return true;
            }
        }

        var wideStart = Math.Max(0, lineIndex - 100);
        for (var i = lineIndex; i >= wideStart; i--)
        {
            var context = lines[i].ToLowerInvariant();
            if (context.Contains("\"forbiddenclaims\"", StringComparison.Ordinal) ||
                context.Contains("\"nonclaims\"", StringComparison.Ordinal))
            {
                return true;
            }
        }

        for (var i = lineIndex; i >= 0; i--)
        {
            var headerLine = lines[i].Trim().ToLowerInvariant();
            if (!headerLine.StartsWith("## ", StringComparison.Ordinal))
                continue;

            if (headerLine.Contains("forbidden claims", StringComparison.Ordinal) ||
                headerLine.Contains("non-claims", StringComparison.Ordinal))
            {
                return true;
            }

            break;
        }

        var prefix = lowerLine[..matchIndex];
        return Regex.IsMatch(prefix, @"\bno\s+$", RegexOptions.CultureInvariant);
    }

    private static bool IsAllowedLineOnlyContext(string line)
    {
        return line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Non-claims", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Forbidden claims", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Disabled boundary", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Current boundary", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Intentionally disabled capabilities", StringComparison.OrdinalIgnoreCase);
    }
}
