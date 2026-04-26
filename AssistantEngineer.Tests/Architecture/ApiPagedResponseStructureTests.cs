using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiPagedResponseStructureTests
{
    [Fact]
    public void ApiHasCentralizedPagedResultExtension()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Results.PagedResultExtensions");

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
    public void ControllersDoNotBuildPagedOkResponsesManually()
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
            "Ok(items.ToPagedResponse(query))",
            "Ok(items.ToPagedResponse(",
            ".ToPagedResponse(query)"
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
            $"Controllers must use ToPagedOkResult instead of building paged responses manually: {string.Join(", ", violations)}.");
    }
}