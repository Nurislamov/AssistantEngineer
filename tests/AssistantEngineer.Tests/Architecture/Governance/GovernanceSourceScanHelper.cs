namespace AssistantEngineer.Tests.Architecture.Governance;

internal static class GovernanceSourceScanHelper
{
    private static readonly string[] DefaultForbiddenWritePatterns =
    [
        "SaveChanges(",
        "SaveChangesAsync(",
        "UPDATE ",
        "DELETE ",
        "TRUNCATE ",
        "INSERT INTO"
    ];

    public static void AssertNoWritePatterns(
        string sourceRoot,
        IReadOnlyList<string>? additionalForbiddenPatterns = null,
        IReadOnlyList<string>? skipPathSuffixes = null)
    {
        var patterns = new List<string>(DefaultForbiddenWritePatterns);
        if (additionalForbiddenPatterns is not null)
            patterns.AddRange(additionalForbiddenPatterns);

        var sourceFiles = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => skipPathSuffixes is null || !skipPathSuffixes.Any(suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.NotEmpty(sourceFiles);

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in patterns)
            {
                var comparison = pattern.Any(char.IsLower)
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
                Assert.DoesNotContain(pattern, content, comparison);
            }
        }
    }

    public static void AssertCliApplyDisabled(string cliSource, string disabledMessage)
    {
        Assert.Contains("OwnershipBackfillCommandType.Apply => await ExecuteApplyDisabledAsync", cliSource, StringComparison.Ordinal);
        Assert.Contains(disabledMessage, cliSource, StringComparison.Ordinal);
    }

    public static string ExtractMethodBody(string fileContent, string methodSignaturePrefix)
    {
        var markerIndex = fileContent.IndexOf(methodSignaturePrefix, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Could not locate method marker: {methodSignaturePrefix}");

        var bodyStart = fileContent.IndexOf('{', markerIndex);
        Assert.True(bodyStart >= 0, $"Could not locate method body start for marker: {methodSignaturePrefix}");

        var depth = 0;
        for (var i = bodyStart; i < fileContent.Length; i++)
        {
            if (fileContent[i] == '{')
                depth++;
            else if (fileContent[i] == '}')
                depth--;

            if (depth == 0)
                return fileContent.Substring(bodyStart, i - bodyStart + 1);
        }

        throw new InvalidOperationException($"Failed to parse method body for marker: {methodSignaturePrefix}");
    }
}
