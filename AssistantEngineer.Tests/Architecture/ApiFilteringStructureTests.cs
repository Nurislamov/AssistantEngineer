using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiFilteringStructureTests
{
    [Fact]
    public void ApiHasCentralizedFilterExtensions()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Collections.FilterExtensions");

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
                "ApplyFilter",
                "ApplyNullableValueFilter",
                "ApplyStringEqualsFilter",
                "ApplyValueFilter"
            ],
            methodNames);
    }

    [Fact]
    public void ControllersDoNotUseRepeatedInlineFilterBlocks()
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

                return text.Contains("if (query.", StringComparison.Ordinal) &&
                       text.Contains("items = items.Where", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must use FilterExtensions instead of repeated inline filter blocks: {string.Join(", ", violations)}.");
    }
}
