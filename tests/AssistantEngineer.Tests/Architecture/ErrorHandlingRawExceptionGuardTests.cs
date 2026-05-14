namespace AssistantEngineer.Tests.Architecture;

public sealed class ErrorHandlingRawExceptionGuardTests
{
    [Fact]
    public void BackendSource_DoesNotUseRawThrowNewException()
    {
        var backendRoot = Path.Combine(TestPaths.RepoRoot, "src", "Backend");
        Assert.True(Directory.Exists(backendRoot), $"Backend root was not found: {backendRoot}");

        var violations = new List<string>();
        var regex = new System.Text.RegularExpressions.Regex(
            @"throw\s+new\s+(System\.)?Exception\s*\(",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        foreach (var file in EnumerateBackendSourceFiles(backendRoot))
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                if (!regex.IsMatch(lines[index]))
                    continue;

                violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)}:{index + 1}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Raw throw new Exception is forbidden in backend source. Violations:\n" + string.Join('\n', violations));
    }

    private static IEnumerable<string> EnumerateBackendSourceFiles(string root)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj"
        };

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(segment => excluded.Contains(segment)))
                continue;

            yield return file;
        }
    }
}
