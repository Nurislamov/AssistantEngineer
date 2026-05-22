using System.Reflection;
using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AssistantEngineer.Tests.Api.EngineeringWorkflow;

public sealed class EngineeringWorkflowControllerRouteSignatureTests
{
    [Fact]
    public void WorkflowController_RouteTemplatesRemainStable()
    {
        var methods = typeof(EngineeringWorkflowController).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.PrepareCalculation), "prepare-calculation");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.RunCalculation), "run-calculation");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.CreateOrRunJob), "jobs");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.CancelJob), "jobs/{jobId}/cancel");

        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GetWorkflowState), "{projectId:int}/state");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GetScenarioResult), "scenarios/{scenarioId}");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GetProjectScenarios), "{projectId:int}/scenarios");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GetJob), "jobs/{jobId}");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GetJobEvents), "jobs/{jobId}/events");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.ListProjectJobs), "{projectId:int}/jobs");

        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.TracePreview), "trace-preview");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GenerateReport), "report");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.ExportReportJson), "report/export/json");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.ExportReportMarkdown), "report/export/markdown");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GetScenarioArtifacts), "scenarios/{scenarioId}/artifacts");
        AssertHttpTemplate(methods, nameof(EngineeringWorkflowController.GetScenarioArtifactByKind), "scenarios/{scenarioId}/artifacts/{artifactKind}");
    }

    [Fact]
    public void WorkflowController_ActionSignaturesRemainCharacterized()
    {
        AssertActionSignature(
            nameof(EngineeringWorkflowController.PrepareCalculation),
            expectedReturnType: "Task`1",
            expectedParameterTypeNames: ["EngineeringWorkflowCalculationPreparationRequestDto", "CancellationToken"]);

        AssertActionSignature(
            nameof(EngineeringWorkflowController.RunCalculation),
            expectedReturnType: "Task`1",
            expectedParameterTypeNames: ["EngineeringCalculationScenarioRequestDto", "String", "CancellationToken"]);

        AssertActionSignature(
            nameof(EngineeringWorkflowController.CreateOrRunJob),
            expectedReturnType: "Task`1",
            expectedParameterTypeNames: ["EngineeringCalculationJobRequestDto", "String", "CancellationToken"]);

        AssertActionSignature(
            nameof(EngineeringWorkflowController.CancelJob),
            expectedReturnType: "Task`1",
            expectedParameterTypeNames: ["String", "CancellationToken"]);

        AssertActionSignature(
            nameof(EngineeringWorkflowController.GetWorkflowState),
            expectedReturnType: "Task`1",
            expectedParameterTypeNames: ["Int32", "Nullable`1", "CancellationToken"]);
    }

    [Fact]
    public void WorkflowController_DtoBindingPointsRemainStable()
    {
        var methods = typeof(EngineeringWorkflowController).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var prepare = methods.Single(method => method.Name == nameof(EngineeringWorkflowController.PrepareCalculation));
        var run = methods.Single(method => method.Name == nameof(EngineeringWorkflowController.RunCalculation));
        var createJob = methods.Single(method => method.Name == nameof(EngineeringWorkflowController.CreateOrRunJob));
        var trace = methods.Single(method => method.Name == nameof(EngineeringWorkflowController.TracePreview));
        var report = methods.Single(method => method.Name == nameof(EngineeringWorkflowController.GenerateReport));

        Assert.Equal(typeof(EngineeringWorkflowCalculationPreparationRequestDto), prepare.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(EngineeringCalculationScenarioRequestDto), run.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(EngineeringCalculationJobRequestDto), createJob.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(EngineeringWorkflowTracePreviewRequestDto), trace.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(EngineeringWorkflowReportRequestDto), report.GetParameters()[0].ParameterType);
    }

    private static void AssertActionSignature(
        string actionName,
        string expectedReturnType,
        IReadOnlyList<string> expectedParameterTypeNames)
    {
        var method = typeof(EngineeringWorkflowController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(candidate => candidate.Name == actionName);

        Assert.Contains(expectedReturnType, method.ReturnType.Name, StringComparison.Ordinal);
        var parameterTypeNames = method.GetParameters()
            .Select(parameter => parameter.ParameterType.Name)
            .ToArray();

        Assert.Equal(expectedParameterTypeNames, parameterTypeNames);
    }

    private static void AssertHttpTemplate(
        IReadOnlyList<MethodInfo> methods,
        string actionName,
        string expectedTemplate)
    {
        var method = methods.Single(candidate => candidate.Name == actionName);
        var templates = method.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Contains(templates, template => string.Equals(template, expectedTemplate, StringComparison.Ordinal));
    }
}
