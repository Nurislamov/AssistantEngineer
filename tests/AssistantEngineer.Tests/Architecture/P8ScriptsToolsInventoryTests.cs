using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8ScriptsToolsInventoryTests
{
    [Fact]
    public void ScriptsToolsInventoryArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            InventoryMarkdownPath,
            InventoryJsonPath,
            InventorySchemaPath);
    }

    [Fact]
    public void ScriptsToolsInventoryUsesKnownCategoriesAndCriticalClassifications()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "KeepAsWrapper",
            "ConvertToToolCandidate",
            "DeprecatedCandidate",
            "ReleaseGateCritical",
            "GeneratedArtifactProducer",
            "ToolingCritical",
            "UnknownNeedsReview"
        };

        var entries = root.GetProperty("entries").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);

        foreach (var entry in entries)
        {
            var category = entry.GetProperty("category").GetString() ?? string.Empty;
            Assert.Contains(category, allowed);

            var path = entry.GetProperty("path").GetString() ?? string.Empty;
            Assert.DoesNotContain("deleted", path, StringComparison.OrdinalIgnoreCase);
            Assert.True(entry.TryGetProperty("safeToRemove", out _), "Every inventory entry must include safeToRemove.");
        }

        Assert.Contains(entries, entry =>
            string.Equals(NormalizePath(entry.GetProperty("path").GetString()), "scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1", StringComparison.Ordinal) &&
            string.Equals(entry.GetProperty("category").GetString(), "ReleaseGateCritical", StringComparison.Ordinal));

        Assert.Contains(entries, entry =>
            string.Equals(NormalizePath(entry.GetProperty("path").GetString()), "tools/AssistantEngineer.Tools.OwnershipBackfill/AssistantEngineer.Tools.OwnershipBackfill.csproj", StringComparison.Ordinal) &&
            string.Equals(entry.GetProperty("category").GetString(), "ToolingCritical", StringComparison.Ordinal));

        Assert.Contains(entries, entry =>
            string.Equals(NormalizePath(entry.GetProperty("path").GetString()), "tools/AssistantEngineer.Tools.EngineeringCoreRelease/AssistantEngineer.Tools.EngineeringCoreRelease.csproj", StringComparison.Ordinal) &&
            (string.Equals(entry.GetProperty("category").GetString(), "ToolingCritical", StringComparison.Ordinal) ||
             string.Equals(entry.GetProperty("category").GetString(), "ReleaseGateCritical", StringComparison.Ordinal)));
    }

    [Fact]
    public void EveryScriptAndToolProjectIsRepresented()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var entries = document.RootElement.GetProperty("entries").EnumerateArray().ToArray();
        var paths = entries
            .Select(entry => NormalizePath(entry.GetProperty("path").GetString()))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scriptPaths = Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, "scripts"), "*.ps1", SearchOption.AllDirectories)
            .Select(path => NormalizePath(Path.GetRelativePath(TestPaths.RepoRoot, path)))
            .ToArray();
        Assert.NotEmpty(scriptPaths);
        foreach (var script in scriptPaths)
            Assert.Contains(script, paths);

        var toolProjects = Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, "tools"), "AssistantEngineer.Tools.*.csproj", SearchOption.AllDirectories)
            .Select(path => NormalizePath(Path.GetRelativePath(TestPaths.RepoRoot, path)))
            .ToArray();
        Assert.NotEmpty(toolProjects);
        foreach (var tool in toolProjects)
            Assert.Contains(tool, paths);
    }

    [Fact]
    public void SafeToRemoveCannotBeTrueWithoutDeprecatedCategoryAndReason()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        foreach (var entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            var safeToRemove = entry.GetProperty("safeToRemove").GetBoolean();
            if (!safeToRemove)
                continue;

            Assert.Equal("DeprecatedCandidate", entry.GetProperty("category").GetString());
            var reason = entry.GetProperty("recommendedAction").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(reason));
        }
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.schema.json");

    private static string NormalizePath(string? path) =>
        (path ?? string.Empty).Replace('\\', '/');
}
