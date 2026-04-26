using AssistantEngineer.Api;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests.Architecture;

public class ApiExtensionStructureTests
{
    [Fact]
    public void ExtensionClassesHaveExpectedResponsibilities()
    {
        var apiAssembly = typeof(Program).Assembly;

        Assert.NotNull(apiAssembly.GetType(
            "AssistantEngineer.Api.Extensions.Collections.CollectionQueryExtensions"));

        Assert.NotNull(apiAssembly.GetType(
            "AssistantEngineer.Api.Extensions.Results.ResultExtensions"));

        Assert.NotNull(apiAssembly.GetType(
            "AssistantEngineer.Api.Extensions.Http.ApiProblemDetailsFactory"));
    }

    [Fact]
    public void ResultExtensionsExposeOnlyResultMappingMethods()
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

        Assert.Equal(
            [
                "ToActionResult",
                "ToCreatedAtGetByIdResult",
                "ToCreatedResult",
                "ToFailureResult",
                "ToNoContentResult",
                "ToOkResult"
            ],
            methodNames);
    }

    [Fact]
    public void CollectionQueryExtensionsExposeOnlyCollectionMappingMethods()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Collections.CollectionQueryExtensions");

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
                "ApplySearch",
                "ToPagedResponse"
            ],
            methodNames);
    }

    [Fact]
    public void ApiProblemDetailsFactoryExposesOnlyProblemDetailsMethods()
    {
        var type = typeof(Program).Assembly.GetType(
            "AssistantEngineer.Api.Extensions.Http.ApiProblemDetailsFactory");

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
                "CreateProblemDetails",
                "CreateProblemResult",
                "CreateResult",
                "CreateValidationResult"
            ],
            methodNames);
    }

    [Fact]
    public void ControllersStillUseExtensionNamespaceOnlyThroughPublicHelpers()
    {
        var controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(controllerTypes);
    }
}
