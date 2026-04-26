using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiSortingStructureTests
{
    [Fact]
    public void ApiHasCentralizedSortExtensions()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Collections.SortExtensions");

        Assert.NotNull(type);

        var methodNames = type
            .GetMethods()
            .Where(method =>
                method.DeclaringType == type &&
                !method.IsSpecialName)
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ApplySort",
                "SortBy",
                "ThenByStable"
            ],
            methodNames);
    }

    [Fact]
    public void ControllersDoNotUseInlineSortSwitches()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

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

                return text.Contains("ToLowerInvariant() switch", StringComparison.Ordinal) ||
                       text.Contains("SortDescending ? source.OrderByDescending", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must use SortExtensions instead of inline sorting switches: {string.Join(", ", violations)}.");
    }
}
