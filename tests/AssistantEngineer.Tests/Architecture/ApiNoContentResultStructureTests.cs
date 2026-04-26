using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiNoContentResultStructureTests
{
    [Fact]
    public void ApiHasCentralizedNoContentResultMapping()
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
            "ToNoContentResult",
            methodNames);
    }

    [Fact]
    public void ControllersDoNotMapSuccessfulResultToNoContentManually()
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

                return text.Contains("if (result.IsSuccess)", StringComparison.Ordinal) &&
                       text.Contains("return NoContent();", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must use Result.ToNoContentResult instead of manual NoContent mapping: {string.Join(", ", violations)}.");
    }
}