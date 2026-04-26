using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiProblemDetailsStructureTests
{
    [Fact]
    public void ProblemDetailsComponentsLiveInHttpExtensionsNamespace()
    {
        var expectedTypes = new[]
        {
            "AssistantEngineer.Api.Extensions.Http.ApiProblemDetailsFactory",
            "AssistantEngineer.Api.Extensions.Http.ProblemDetailsErrorDescriptor",
            "AssistantEngineer.Api.Extensions.Http.ProblemDetailsErrorMapper",
            "AssistantEngineer.Api.Extensions.Http.ProblemDetailsMetadataExtensions",
            "AssistantEngineer.Api.Extensions.Http.ApiValidationProblemDetailsFactory"
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
    public void ApiProblemDetailsFactoryRemainsPublicFacade()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Http.ApiProblemDetailsFactory");

        Assert.NotNull(type);
        Assert.True(type.IsPublic);

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
                "CreateProblemDetails",
                "CreateProblemResult",
                "CreateResult",
                "CreateValidationResult"
            ],
            methodNames);
    }

    [Fact]
    public void ApiProblemDetailsFactoryDoesNotContainErrorMappingSwitch()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var factoryPath = Path.Combine(
            apiProjectPath,
            "Extensions",
            "Http",
            "ApiProblemDetailsFactory.cs");

        var text = File.ReadAllText(factoryPath);

        Assert.DoesNotContain(
            "ResultErrorType.NotFound",
            text,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "ResultErrorType.Validation",
            text,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "ResultErrorType.Conflict",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ProblemDetailsErrorMapperOwnsResultErrorMapping()
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
            "Extensions",
            "Http",
            "ProblemDetailsErrorMapper.cs");

        var text = File.ReadAllText(mapperPath);

        Assert.Contains(
            "ResultErrorType.NotFound",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "ResultErrorType.Validation",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "ResultErrorType.Conflict",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationProblemDetailsFactoryOwnsValidationProblemCreation()
    {
        var apiProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AssistantEngineer.Api"));

        var validationFactoryPath = Path.Combine(
            apiProjectPath,
            "Extensions",
            "Http",
            "ApiValidationProblemDetailsFactory.cs");

        var text = File.ReadAllText(validationFactoryPath);

        Assert.Contains(
            "new ValidationProblemDetails",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "validation_failed",
            text,
            StringComparison.Ordinal);
    }
}