using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Architecture;

public class ApiValidationResponseStructureTests
{
    [Fact]
    public void ApiUsesCentralizedApiBehaviorOptionsSetup()
    {
        Assert.True(
            typeof(IConfigureOptions<ApiBehaviorOptions>).IsAssignableFrom(typeof(ApiBehaviorOptionsSetup)),
            "ApiBehaviorOptionsSetup must configure ApiBehaviorOptions through IConfigureOptions<ApiBehaviorOptions>.");
    }

    [Fact]
    public void ApiProblemDetailsFactoryOwnsValidationProblemDetailsCreation()
    {
        var type = typeof(ApiProblemDetailsFactory);

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
            "CreateValidationResult",
            methodNames);
    }

    [Fact]
    public void CompositionRootDoesNotConfigureInvalidModelStateResponseInline()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var programPath = Path.Combine(
            apiProjectPath,
            "Program.cs");

        var text = File.ReadAllText(programPath);

        Assert.DoesNotContain(
            "InvalidModelStateResponseFactory",
            text,
            StringComparison.Ordinal);

        var registrationPath = Path.Combine(
            apiProjectPath,
            "Configuration",
            "ApiPresentationRegistration.cs");

        var registrationText = File.ReadAllText(registrationPath);

        Assert.Contains(
            "ConfigureOptions<ApiBehaviorOptionsSetup>",
            registrationText,
            StringComparison.Ordinal);
    }
}
