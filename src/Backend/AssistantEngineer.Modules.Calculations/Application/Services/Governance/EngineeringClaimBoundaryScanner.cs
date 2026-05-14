using AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Governance;

public sealed class EngineeringClaimBoundaryScanner
{
    private static readonly IReadOnlyList<string> DefaultForbiddenTokens =
    [
        "full ISO compliance",
        "full EN compliance",
        "full ISO 52016 compliance",
        "full ISO 52010 compliance",
        "full ISO 13370 compliance",
        "full ISO 16798 compliance",
        "full ISO 12831-3 compliance",
        "full EN 15316 compliance",
        "ISO 52016 validated",
        "ISO 52010 validated",
        "ISO 13370 validated",
        "ISO 16798 validated",
        "ISO 12831-3 validated",
        "EN 15316 validated",
        "validated against StandardReference",
        "validated against EnergyPlus",
        "exact EnergyPlus equivalence",
        "exact EnergyPlus numerical equivalence",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 " + "validated",
        "ASHRAE 140 / BESTEST-style validated",
        "ASHRAE 140 covered",
        "ExternalReferenceCovered",
        "certified",
        "external certification"
    ];

    private static readonly IReadOnlyList<string> AllowedNegationTokens =
    [
        "no ",
        "not ",
        "must not",
        "without",
        "not a",
        "not an",
        "is not",
        "are not"
    ];

    private static readonly HashSet<string> ExcludedSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
        "TestResults",
        "node_modules",
        ".vs",
        "claim-scanner-inputs"
    };

    public IReadOnlyList<string> GetDefaultForbiddenTokens() => DefaultForbiddenTokens;

    public EngineeringGovernanceCheckResult ScanRepository(
        string? repositoryRoot = null,
        IReadOnlyList<string>? forbiddenTokens = null,
        IReadOnlyList<string>? allowedNegationTokens = null,
        IReadOnlyList<string>? explicitFiles = null)
    {
        var repoRoot = ResolveRepositoryRoot(repositoryRoot);
        var tokens = forbiddenTokens ?? DefaultForbiddenTokens;
        var negations = allowedNegationTokens ?? AllowedNegationTokens;

        var diagnostics = new List<EngineeringGovernanceCheckDiagnostic>();
        var scannedFiles = explicitFiles is { Count: > 0 }
            ? explicitFiles
                .Select(path => NormalizeToAbsolute(repoRoot, path))
                .Where(File.Exists)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : EnumerateScanFiles(repoRoot).ToArray();

        foreach (var filePath in scannedFiles)
        {
            var relativePath = NormalizeRelativePath(repoRoot, filePath);
            var lines = File.ReadAllLines(filePath);

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var previousLine = GetPreviousNonEmptyLine(lines, index);

                foreach (var token in tokens)
                {
                    if (!line.Contains(token, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (IsAllowedNegatedContext(lines, index, line, previousLine, token, negations))
                        continue;

                    diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                        Code: "Governance.ClaimScanner.ForbiddenPositiveClaimDetected",
                        Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                        Message: $"Forbidden positive claim token detected: {token}",
                        FilePath: relativePath,
                        LineNumber: index + 1,
                        Token: token));
                }
            }
        }

        var totalChecks = scannedFiles.Length;
        var passedChecks = totalChecks - diagnostics.Select(item => item.FilePath).Where(path => path is not null).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var warningCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Warning);
        var errorCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Error);
        var criticalCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Critical);

        var readiness = errorCount > 0 || criticalCount > 0
            ? EngineeringGovernanceReleaseReadinessStatus.Blocked
            : warningCount > 0
                ? EngineeringGovernanceReleaseReadinessStatus.ReadyWithWarnings
                : EngineeringGovernanceReleaseReadinessStatus.Ready;

        return new EngineeringGovernanceCheckResult(
            CheckId: "EngineeringClaimBoundaryScan",
            ReadinessStatus: readiness,
            TotalChecks: totalChecks,
            PassedChecks: Math.Max(0, passedChecks),
            WarningCount: warningCount,
            ErrorCount: errorCount,
            CriticalCount: criticalCount,
            Diagnostics: diagnostics,
            StageSummaries: scannedFiles.Select(path => NormalizeRelativePath(repoRoot, path)).ToArray());
    }

    private static bool IsAllowedNegatedContext(
        IReadOnlyList<string> lines,
        int lineIndex,
        string line,
        string previousLine,
        string token,
        IReadOnlyList<string> allowedNegationTokens)
    {
        var normalized = line.ToLowerInvariant();
        var normalizedPrevious = previousLine.ToLowerInvariant();

        if (IsNonClaimsContext(lines, lineIndex))
            return true;

        if (string.Equals(token, "certified", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(token, "external certification", StringComparison.OrdinalIgnoreCase))
            return HasAnyNegation(normalized, allowedNegationTokens);

        if (string.Equals(token, "ExternalReferenceCovered", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("no ", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("not ", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("must not", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("requires", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("support", StringComparison.OrdinalIgnoreCase);
        }

        if (HasAnyNegation(normalized, allowedNegationTokens))
            return true;

        return normalizedPrevious.Contains("does not mean", StringComparison.OrdinalIgnoreCase) ||
            normalizedPrevious.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
            normalizedPrevious.Contains("incorrect examples", StringComparison.OrdinalIgnoreCase) ||
            normalizedPrevious.Contains("not marked as", StringComparison.OrdinalIgnoreCase) ||
            normalizedPrevious.Contains("out of scope", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnyNegation(string normalizedLine, IReadOnlyList<string> allowedNegationTokens)
    {
        if (allowedNegationTokens.Any(negation => normalizedLine.Contains(negation, StringComparison.OrdinalIgnoreCase)))
            return true;

        return normalizedLine.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
            normalizedLine.Contains("does not mean", StringComparison.OrdinalIgnoreCase) ||
            normalizedLine.Contains("incorrect examples", StringComparison.OrdinalIgnoreCase) ||
            normalizedLine.Contains(": no", StringComparison.OrdinalIgnoreCase) ||
            normalizedLine.Contains("= no", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNonClaimsContext(IReadOnlyList<string> lines, int lineIndex)
    {
        const int contextWindow = 8;
        var from = Math.Max(0, lineIndex - contextWindow);

        for (var i = lineIndex; i >= from; i--)
        {
            var normalized = lines[i].ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            if (normalized.Contains("does not claim", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("do not claim", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("must not claim", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("should not claim", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("not claim", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("non-claim", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("non claim", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> EnumerateScanFiles(string repositoryRoot)
    {
        var docsRoot = Path.Combine(repositoryRoot, "docs");
        if (Directory.Exists(docsRoot))
        {
            foreach (var path in Directory.EnumerateFiles(docsRoot, "*", SearchOption.AllDirectories))
            {
                if (!IsJsonOrMarkdown(path))
                    continue;

                if (IsExcludedPath(path, repositoryRoot))
                    continue;

                yield return path;
            }
        }

        var fixturesRoot = Path.Combine(repositoryRoot, "tests", "fixtures");
        if (Directory.Exists(fixturesRoot))
        {
            foreach (var path in Directory.EnumerateFiles(fixturesRoot, "*.json", SearchOption.AllDirectories))
            {
                if (IsExcludedPath(path, repositoryRoot))
                    continue;

                yield return path;
            }
        }
    }

    private static bool IsJsonOrMarkdown(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcludedPath(string absolutePath, string repositoryRoot)
    {
        var relative = NormalizeRelativePath(repositoryRoot, absolutePath);
        var segments = relative.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => ExcludedSegments.Contains(segment));
    }

    private static string ResolveRepositoryRoot(string? explicitRepositoryRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRepositoryRoot))
            return Path.GetFullPath(explicitRepositoryRoot);

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AssistantEngineer.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing AssistantEngineer.sln was not found.");
    }

    private static string NormalizeToAbsolute(string repositoryRoot, string path)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string NormalizeRelativePath(string repositoryRoot, string absoluteOrRelativePath)
    {
        var fullRoot = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(absoluteOrRelativePath);

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(fullRoot.Length).Replace(Path.DirectorySeparatorChar, '/');

        return absoluteOrRelativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string GetPreviousNonEmptyLine(IReadOnlyList<string> lines, int index)
    {
        for (var cursor = index - 1; cursor >= 0; cursor--)
        {
            var line = lines[cursor];
            if (!string.IsNullOrWhiteSpace(line))
                return line;
        }

        return string.Empty;
    }
}
