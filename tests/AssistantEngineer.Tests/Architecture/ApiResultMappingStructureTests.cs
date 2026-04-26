using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiResultMappingStructureTests
{
    [Fact]
    public void ApiHasCentralizedPagedResultMappingExtensions()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Results.PagedResultMappingExtensions");

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
                "ToPagedOkResult"
            ],
            methodNames);
    }

    [Fact]
    public void ControllersDoNotMapPagedResultFailuresManually()
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

                return text.Contains("if (result.IsFailure)", StringComparison.Ordinal) &&
                       text.Contains("return ApiProblemDetailsFactory.CreateResult", StringComparison.Ordinal) &&
                       text.Contains("PagedResponse<", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Paged endpoints must use Result.ToPagedOkResult instead of manual failure mapping: {string.Join(", ", violations)}.");
    }
}