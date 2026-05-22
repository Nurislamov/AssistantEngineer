using AssistantEngineer.Tests.Architecture.Governance;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture;

public sealed partial class ScriptsToolsInventoryCoverageTests
{
    [Fact]
    public void WorkflowReferencedScriptsAndToolsAreKnownAndNotDeprecated()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var entries = document.RootElement.GetProperty("entries").EnumerateArray().ToArray();
        var pathToCategory = entries.ToDictionary(
            entry => NormalizePath(entry.GetProperty("path").GetString()),
            entry => entry.GetProperty("category").GetString() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var workflows = Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, ".github", "workflows"), "*.yml", SearchOption.TopDirectoryOnly);
        Assert.NotEmpty(workflows);

        foreach (var workflowPath in workflows)
        {
            var content = File.ReadAllText(workflowPath);

            var scriptRefs = ScriptPathRegex()
                .Matches(content)
                .Select(match => NormalizePath(match.Groups["path"].Value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var script in scriptRefs)
            {
                Assert.Contains(script, pathToCategory.Keys);
                Assert.NotEqual("DeprecatedCandidate", pathToCategory[script]);
            }

            var toolRefs = ToolProjectRegex()
                .Matches(content)
                .Select(match => NormalizePath(match.Groups["path"].Value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var tool in toolRefs)
            {
                Assert.Contains(tool, pathToCategory.Keys);
                Assert.NotEqual("DeprecatedCandidate", pathToCategory[tool]);
            }
        }
    }

    [Fact]
    public void ReleaseReadyWorkflowScriptIsReleaseGateCritical()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var entries = document.RootElement.GetProperty("entries").EnumerateArray().ToArray();

        Assert.Contains(entries, entry =>
            string.Equals(NormalizePath(entry.GetProperty("path").GetString()), "scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(entry.GetProperty("category").GetString(), "ReleaseGateCritical", StringComparison.Ordinal));
    }

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json");

    [GeneratedRegex(@"(?<path>scripts[\\/][^\s""']+?\.ps1)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptPathRegex();

    [GeneratedRegex(@"(?<path>tools[\\/][^\s""']+?\.csproj)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ToolProjectRegex();

    private static string NormalizePath(string? path) =>
        (path ?? string.Empty).Replace('\\', '/');
}
