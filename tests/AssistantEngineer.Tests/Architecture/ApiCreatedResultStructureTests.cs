using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiCreatedResultStructureTests
{
    [Fact]
    public void ApiHasCentralizedCreatedAtGetByIdResultMapping()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Results.ResultExtensions");

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

        Assert.Contains(
            "ToCreatedAtGetByIdResult",
            methodNames);
    }

    [Fact]
    public void ControllersDoNotRepeatCreatedAtGetByIdActionName()
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

                return text.Contains("ToCreatedResult(", StringComparison.Ordinal) &&
                       text.Contains("nameof(GetById)", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must use ToCreatedAtGetByIdResult instead of repeating nameof(GetById): {string.Join(", ", violations)}.");
    }
}