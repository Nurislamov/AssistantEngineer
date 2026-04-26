using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiSearchingRulesStructureTests
{
    [Fact]
    public void SearchingRulesLiveInDedicatedNamespaces()
    {
        var allowedNamespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "AssistantEngineer.Api.Searching.Projects",
            "AssistantEngineer.Api.Searching.Buildings",
            "AssistantEngineer.Api.Searching.Equipment"
        };

        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type.Name.EndsWith("SearchExtensions", StringComparison.Ordinal))
            .Where(type =>
                type.Namespace is null ||
                !allowedNamespaces.Contains(type.Namespace))
            .Select(type => $"{type.FullName} has invalid namespace '{type.Namespace}'.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Search helpers must live in dedicated searching namespaces: {string.Join("; ", violations)}.");
    }

    [Fact]
    public void ControllersDoNotUseLowLevelApplySearchDirectly()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var controllerFiles = Directory
            .EnumerateFiles(
                Path.Combine(apiProjectPath, "Controllers"),
                "*.cs",
                SearchOption.AllDirectories)
            .ToArray();

        var violations = controllerFiles
            .Where(path =>
            {
                var text = File.ReadAllText(path);

                return text.Contains(".ApplySearch(", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must use feature search helpers instead of ApplySearch directly: {string.Join(", ", violations)}.");
    }
}