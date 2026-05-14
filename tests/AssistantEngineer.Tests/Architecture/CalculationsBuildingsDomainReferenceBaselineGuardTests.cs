namespace AssistantEngineer.Tests.Architecture;

public sealed class CalculationsBuildingsDomainReferenceBaselineGuardTests
{
    private const string ForbiddenNamespace = "AssistantEngineer.Modules.Buildings.Domain";
    private const string AllowlistPath = "tests/fixtures/architecture/calculations-buildings-domain-reference-allowlist.txt";

    [Fact]
    public void CalculationsModule_BuildingsDomainReferences_StayWithinBaselineAllowlist()
    {
        var calculationsRoot = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations");

        var actualReferences = Directory
            .EnumerateFiles(calculationsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(ForbiddenNamespace, StringComparison.Ordinal))
            .Select(ToRepoRelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var allowlist = ReadAllowlist(AllowlistPath);

        var unexpected = actualReferences
            .Where(path => !allowlist.Contains(path))
            .ToArray();

        var staleAllowlistEntries = allowlist
            .Where(path => !actualReferences.Contains(path, StringComparer.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            unexpected.Length == 0,
            "New Calculations -> Buildings.Domain references are forbidden until decoupling snapshots are introduced. " +
            $"Additions: {string.Join(", ", unexpected)}");

        Assert.True(
            staleAllowlistEntries.Length == 0,
            "Calculations -> Buildings.Domain allowlist contains stale entries. " +
            $"Remove stale paths from {AllowlistPath}: {string.Join(", ", staleAllowlistEntries)}");
    }

    private static HashSet<string> ReadAllowlist(string relativePath)
    {
        var absolutePath = Path.Combine(TestPaths.RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(absolutePath), $"Allowlist file is missing: {absolutePath}");

        return File.ReadAllLines(absolutePath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .Select(NormalizePath)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string ToRepoRelativePath(string absolutePath)
    {
        var relative = Path.GetRelativePath(TestPaths.RepoRoot, absolutePath);
        return NormalizePath(relative);
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/');
}
