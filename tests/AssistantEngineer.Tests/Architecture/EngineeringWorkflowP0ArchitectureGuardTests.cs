using System.Reflection;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Workflow;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AssistantEngineer.Tests.Architecture;

public class EngineeringWorkflowP0ArchitectureGuardTests
{
    [Fact]
    public void EngineeringWorkflowControllerRemainsThinAfterP0Extraction()
    {
        var controllerPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Controllers",
            "Calculations",
            "EngineeringWorkflowController.cs");

        var lines = File.ReadAllLines(controllerPath);
        var effectiveLineCount = lines.Count(line => !string.IsNullOrWhiteSpace(line));

        Assert.True(
            effectiveLineCount <= 450,
            $"EngineeringWorkflowController must remain a thin HTTP adapter after P0 extraction. " +
            $"Current non-blank line count: {effectiveLineCount}; expected <= 450.");
    }

    [Fact]
    public void EngineeringWorkflowControllerDoesNotReintroduceStateBuilderImplementationDetails()
    {
        var controllerText = File.ReadAllText(Path.Combine(
            TestPaths.ApiProjectPath,
            "Controllers",
            "Calculations",
            "EngineeringWorkflowController.cs"));

        var forbiddenFragments = new[]
        {
            "private static readonly string[] WorkflowSteps",
            "private static readonly string[] AvailableModules",
            "private const int DefaultWeatherYear",
            "private async Task<ActionResult<EngineeringWorkflowStateDto>> BuildWorkflowStateAsync",
            "private async Task<EngineeringWorkflowStateDto> BuildWorkflowStateAsync",
            "private EngineeringWorkflowStateDto BuildInfrastructureFallbackState",
            "private IReadOnlyList<EngineeringWorkflowDiagnosticDto> ValidateState",
            "private IReadOnlyList<EngineeringWorkflowStepStatusDto> BuildStepStatuses",
            "private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics",
            "private static bool IsErrorSeverity"
        };

        var violations = forbiddenFragments
            .Where(fragment => controllerText.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "EngineeringWorkflowController must not reabsorb extracted workflow implementation details: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void EngineeringWorkflowControllerDoesNotDependOnBuildingOrCoreFacadesDirectly()
    {
        var constructor = Assert.Single(typeof(EngineeringWorkflowController).GetConstructors());
        var dependencies = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.DoesNotContain(typeof(IBuildingsFacade), dependencies);
        Assert.DoesNotContain(typeof(IEngineeringCoreStatusFacade), dependencies);
        Assert.Contains(typeof(IEngineeringWorkflowStateBuilder), dependencies);
        Assert.Contains(typeof(IEngineeringWorkflowDiagnosticsService), dependencies);
        Assert.Contains(typeof(IEngineeringWorkflowTracePreviewService), dependencies);
        Assert.Contains(typeof(IEngineeringWorkflowReportPreviewService), dependencies);
        Assert.Contains(typeof(IEngineeringCalculationScenarioRunner), dependencies);
        Assert.Contains(typeof(IEngineeringCalculationJobService), dependencies);
        Assert.Contains(typeof(IEngineeringWorkflowPersistenceService), dependencies);
        Assert.Contains(typeof(IEngineeringReportJsonExporter), dependencies);
        Assert.Contains(typeof(IEngineeringReportMarkdownExporter), dependencies);
    }

    [Fact]
    public void EngineeringWorkflowCalculationExecutionEndpointsUseLongRunningTimeoutPolicy()
    {
        AssertUsesLongRunningTimeoutPolicy(nameof(EngineeringWorkflowController.RunCalculation));
        AssertUsesLongRunningTimeoutPolicy(nameof(EngineeringWorkflowController.CreateOrRunJob));
    }

    [Fact]
    public void EngineeringWorkflowActionsDoNotDeclareDuplicateRouteAttributes()
    {
        var duplicates = typeof(EngineeringWorkflowController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => new
            {
                Method = method.Name,
                Routes = method
                    .GetCustomAttributes<HttpMethodAttribute>(inherit: false)
                    .Select(attribute => $"{attribute.HttpMethods.SingleOrDefault() ?? "ANY"}:{attribute.Template ?? string.Empty}")
                    .ToArray()
            })
            .Where(item => item.Routes.Length != item.Routes.Distinct(StringComparer.Ordinal).Count())
            .Select(item => item.Method)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            "Engineering workflow actions must not declare duplicate route attributes: " +
            string.Join(", ", duplicates));
    }

    private static void AssertUsesLongRunningTimeoutPolicy(string actionName)
    {
        var method = typeof(EngineeringWorkflowController).GetMethod(actionName);

        Assert.NotNull(method);
        var attribute = Assert.Single(method.GetCustomAttributes<RequestTimeoutAttribute>());
        Assert.Equal(RequestPolicies.LongRunning, attribute.PolicyName);
    }
}