using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AssistantEngineer.Tests.Api;

public class EngineeringWorkflowApiControllerTests
{
    [Fact]
    public void ControllerDeclaresVersionedRoute()
    {
        var route = Assert.Single(typeof(EngineeringWorkflowController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>());

        Assert.Equal("api/v{version:apiVersion}/engineering-workflow", route.Template);

        var versions = typeof(EngineeringWorkflowController)
            .GetCustomAttributes(typeof(ApiVersionAttribute), false)
            .Cast<ApiVersionAttribute>()
            .SelectMany(attribute => attribute.Versions)
            .ToArray();

        Assert.Contains(versions, version => version.MajorVersion == 1 && version.MinorVersion == 0);
    }

    [Fact]
    public void ControllerConstructorDependsOnlyOnWorkflowOrchestrationServices()
    {
        var constructor = Assert.Single(typeof(EngineeringWorkflowController).GetConstructors());
        var dependencies = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(IBuildingsFacade), dependencies);
        Assert.Contains(typeof(IEngineeringCoreStatusFacade), dependencies);
        Assert.Contains(typeof(ICalculationTraceBuilder), dependencies);
        Assert.Contains(typeof(ICalculationTraceSanitizer), dependencies);
        Assert.Contains(typeof(IEngineeringReportBuilder), dependencies);
        Assert.Contains(typeof(IEngineeringReportJsonExporter), dependencies);
        Assert.Contains(typeof(IEngineeringReportMarkdownExporter), dependencies);
        Assert.Contains(typeof(IEngineeringCalculationScenarioRunner), dependencies);
        Assert.Contains(typeof(IEngineeringCalculationJobService), dependencies);
        Assert.Contains(typeof(IEngineeringWorkflowPersistenceService), dependencies);
        Assert.Equal(10, dependencies.Length);
    }

    [Fact]
    public void ControllerActionsExposeWorkflowFoundationEndpoints()
    {
        var actions = typeof(EngineeringWorkflowController).GetMethods();

        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GetWorkflowState), "{projectId:int}/state");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.Validate), "validate");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.PrepareCalculation), "prepare-calculation");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.RunCalculation), "run-calculation");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.CreateOrRunJob), "jobs");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GetJob), "jobs/{jobId}");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GetJobEvents), "jobs/{jobId}/events");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.CancelJob), "jobs/{jobId}/cancel");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.ListProjectJobs), "{projectId:int}/jobs");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GetScenarioResult), "scenarios/{scenarioId}");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GetProjectScenarios), "{projectId:int}/scenarios");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GetScenarioArtifacts), "scenarios/{scenarioId}/artifacts");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GetScenarioArtifactByKind), "scenarios/{scenarioId}/artifacts/{artifactKind}");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.TracePreview), "trace-preview");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.GenerateReport), "report");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.ExportReportJson), "report/export/json");
        AssertActionHasRoute(actions, nameof(EngineeringWorkflowController.ExportReportMarkdown), "report/export/markdown");
    }

    private static void AssertActionHasRoute(
        IReadOnlyList<System.Reflection.MethodInfo> methods,
        string actionName,
        string template)
    {
        var method = methods.Single(item => item.Name == actionName);
        var routes = method.GetCustomAttributes(typeof(HttpMethodAttribute), false)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(routes, route => string.Equals(route, template, StringComparison.Ordinal));
    }
}
