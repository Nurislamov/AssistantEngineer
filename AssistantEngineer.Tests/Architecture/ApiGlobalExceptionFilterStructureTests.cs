using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiGlobalExceptionFilterStructureTests
{
    [Fact]
    public void GlobalExceptionFilterIsThinOrchestrator()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Filters.GlobalExceptionFilter");

        Assert.NotNull(type);

        var declaredMethods = type
            .GetMethods()
            .Where(method =>
                method.DeclaringType == type &&
                !method.IsSpecialName)
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "OnException"
            ],
            declaredMethods);
    }

    [Fact]
    public void ExceptionMappingComponentsLiveInExceptionsNamespace()
    {
        var expectedTypes = new[]
        {
            "AssistantEngineer.Api.Filters.Exceptions.IExceptionProblemDetailsMapper",
            "AssistantEngineer.Api.Filters.Exceptions.ExceptionProblemDetailsMapper"
        };

        var typeNames = typeof(Program).Assembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expectedType in expectedTypes)
        {
            Assert.Contains(
                expectedType,
                typeNames);
        }
    }

    [Fact]
    public void GlobalExceptionFilterDoesNotCreateProblemDetailsDirectly()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var filterPath = Path.Combine(
            apiProjectPath,
            "Filters",
            "GlobalExceptionFilter.cs");

        var text = File.ReadAllText(filterPath);

        var forbiddenFragments = new[]
        {
            "CreateProblemDetails",
            "CreateProblemResult",
            "new ObjectResult",
            "StatusCodes.Status500InternalServerError",
            "exceptionType"
        };

        var violations = forbiddenFragments
            .Where(fragment =>
                text.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"GlobalExceptionFilter must delegate exception mapping details: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ExceptionProblemDetailsMapperOwnsUnexpectedExceptionMapping()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var mapperPath = Path.Combine(
            apiProjectPath,
            "Filters",
            "Exceptions",
            "ExceptionProblemDetailsMapper.cs");

        var text = File.ReadAllText(mapperPath);

        Assert.Contains(
            "unexpected_error",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "StatusCodes.Status500InternalServerError",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "exceptionType",
            text,
            StringComparison.Ordinal);
    }
}