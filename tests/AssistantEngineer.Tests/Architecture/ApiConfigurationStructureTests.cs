using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiConfigurationStructureTests
{
    private static readonly string[] ExpectedConfigurationTypes =
    [
        "AssistantEngineer.Api.Configuration.ApiAuthenticationRegistration",
        "AssistantEngineer.Api.Configuration.ApiBehaviorOptionsSetup",
        "AssistantEngineer.Api.Configuration.ApiConfigurationRegistration",
        "AssistantEngineer.Api.Configuration.ApiDocumentationRegistration",
        "AssistantEngineer.Api.Configuration.ApiPipelineConfiguration",
        "AssistantEngineer.Api.Configuration.ApiPresentationRegistration",
        "AssistantEngineer.Api.Configuration.ApiVersioningRegistration",
        "AssistantEngineer.Api.Configuration.ApplicationModuleRegistration",
        "AssistantEngineer.Api.Configuration.RequestLimitOptions",
        "AssistantEngineer.Api.Configuration.RequestLimitRegistration"
    ];

    [Fact]
    public void ApiStartupConfigurationLivesInConfigurationNamespace()
    {
        var configurationTypeNames = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "AssistantEngineer.Api.Configuration")
            .Select(type => type.FullName ?? type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (var expectedType in ExpectedConfigurationTypes)
        {
            Assert.Contains(
                expectedType,
                configurationTypeNames);
        }
    }

    [Fact]
    public void ApiConfigurationNamespaceDoesNotContainControllersOrFilters()
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "AssistantEngineer.Api.Configuration" &&
                (type.Name.EndsWith("Controller", StringComparison.Ordinal) ||
                 type.Name.EndsWith("Filter", StringComparison.Ordinal)))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Configuration namespace must not contain controllers or filters: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ApiPipelineConfigurationOwnsMiddlewarePipeline()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var pipelineFile = Path.Combine(
            apiProjectPath,
            "Configuration",
            "ApiPipelineConfiguration.cs");

        var text = File.ReadAllText(pipelineFile);

        Assert.Contains(
            "MapOpenApi()",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "UseHttpsRedirection()",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "UseRouting()",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "UseRequestTimeouts()",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "UseAuthentication()",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "UseAuthorization()",
            text,
            StringComparison.Ordinal);

        Assert.True(
            text.IndexOf("UseAuthentication()", StringComparison.Ordinal) <
            text.IndexOf("UseAuthorization()", StringComparison.Ordinal),
            "Authentication middleware must run before authorization middleware.");

        Assert.Contains(
            "MapControllers()",
            text,
            StringComparison.Ordinal);
    }
}