namespace AssistantEngineer.Tests.Architecture;

public sealed class ForbiddenTerminologyGuardTests
{
    [Fact]
    public void PublicAndProductAreas_DoNotContainForbiddenTerminology()
    {
        var roots = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "src"),
            Path.Combine(TestPaths.RepoRoot, "tests"),
            Path.Combine(TestPaths.RepoRoot, "docs"),
            Path.Combine(TestPaths.RepoRoot, "scripts")
        };

        var forbidden = BuildForbiddenTerms();
        var allowedFile = Path.GetFullPath(Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Architecture",
            "ForbiddenTerminologyGuardTests.cs"));

        var violations = new List<string>();
        foreach (var root in roots.Where(Directory.Exists))
        {
            foreach (var file in EnumerateTextFiles(root))
            {
                if (string.Equals(Path.GetFullPath(file), allowedFile, StringComparison.OrdinalIgnoreCase))
                    continue;

                var text = File.ReadAllText(file);
                foreach (var marker in forbidden)
                {
                    if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} => {marker}");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Forbidden terminology was found:\n" + string.Join('\n', violations.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase)));
    }

    private static IReadOnlyList<string> BuildForbiddenTerms()
    {
        var py = "py";
        var be = "BE";
        var pe = "Building";
        var energy = "Energy";

        return new[]
        {
            py + pe + energy,
            py + pe.ToLowerInvariant() + energy.ToLowerInvariant(),
            py + be,
            "donor" + " project",
            "reference" + " donor",
            "donor" + " methodology",
            py + pe + energy + "-style",
            "parity with " + py + pe + energy,
            "full " + "parity",
            "fully " + "validated",
            "EnergyPlus " + "parity",
            "ASHRAE 140 " + "validated",
            py + pe + energy + " parity"
        };
    }

    private static IEnumerable<string> EnumerateTextFiles(string root)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git", "bin", "obj", "node_modules", "dist", "coverage", "TestResults"
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".md", ".json", ".yml", ".yaml", ".ps1", ".txt", ".tsx", ".ts", ".xml", ".csv"
        };

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(segment => excluded.Contains(segment)))
                continue;

            if (!allowedExtensions.Contains(Path.GetExtension(file)))
                continue;

            yield return file;
        }
    }
}
