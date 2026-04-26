using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiFilteringRulesStructureTests
{
    [Fact]
    public void FilteringRulesLiveInDedicatedNamespaces()
    {
        var allowedNamespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "AssistantEngineer.Api.Filtering.Buildings",
            "AssistantEngineer.Api.Filtering.Equipment"
        };

        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type.Name.EndsWith("Filters", StringComparison.Ordinal))
            .Where(type =>
                type.Namespace is null ||
                !allowedNamespaces.Contains(type.Namespace))
            .Select(type => $"{type.FullName} has invalid namespace '{type.Namespace}'.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Filter rules must live in dedicated filtering namespaces: {string.Join("; ", violations)}.");
    }

    [Fact]
    public void ControllersDoNotUseLowLevelFilterExtensionMethodsDirectly()
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

        var forbiddenPatterns = new[]
        {
            ".ApplyValueFilter<",
            ".ApplyNullableValueFilter<",
            ".ApplyStringEqualsFilter("
        };

        var violations = controllerFiles
            .Where(path =>
            {
                var text = File.ReadAllText(path);

                return forbiddenPatterns.Any(pattern =>
                    text.Contains(pattern, StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must use feature filter methods instead of low-level filter helpers: {string.Join(", ", violations)}.");
    }
}