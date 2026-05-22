using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Tests.Api.EngineeringWorkflow;

public sealed class EngineeringWorkflowControllerResponseShapeTests
{
    [Fact]
    public void ControllerActionDtoContracts_RemainStable()
    {
        var methods = typeof(EngineeringWorkflowController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

        AssertActionParameterType(methods, nameof(EngineeringWorkflowController.PrepareCalculation), typeof(EngineeringWorkflowCalculationPreparationRequestDto));
        AssertActionParameterType(methods, nameof(EngineeringWorkflowController.RunCalculation), typeof(EngineeringCalculationScenarioRequestDto));
        AssertActionParameterType(methods, nameof(EngineeringWorkflowController.CreateOrRunJob), typeof(EngineeringCalculationJobRequestDto));
        AssertActionParameterType(methods, nameof(EngineeringWorkflowController.TracePreview), typeof(EngineeringWorkflowTracePreviewRequestDto));
        AssertActionParameterType(methods, nameof(EngineeringWorkflowController.GenerateReport), typeof(EngineeringWorkflowReportRequestDto));
        AssertActionParameterType(methods, nameof(EngineeringWorkflowController.ExportReportJson), typeof(EngineeringWorkflowReportExportRequestDto));
        AssertActionParameterType(methods, nameof(EngineeringWorkflowController.ExportReportMarkdown), typeof(EngineeringWorkflowReportExportRequestDto));
    }

    [Fact]
    public void ReportExportResponseJsonShape_RemainsStable()
    {
        var payload = new EngineeringWorkflowReportExportResponseDto(
            Format: "Json",
            Content: "{}",
            SchemaVersion: "1.0",
            ReportId: "rep-1",
            Diagnostics: []);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var document = JsonDocument.Parse(json);
        var propertyNames = document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("format", propertyNames);
        Assert.Contains("content", propertyNames);
        Assert.Contains("schemaVersion", propertyNames);
        Assert.Contains("reportId", propertyNames);
        Assert.Contains("diagnostics", propertyNames);
    }

    [Fact]
    public void WorkflowStateResponseJsonShape_RemainsStableForCoreFields()
    {
        var payload = EngineeringWorkflowControllerCharacterizationHelper.CreateWorkflowState();
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var document = JsonDocument.Parse(json);
        var propertyNames = document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("projectId", propertyNames);
        Assert.Contains("projectName", propertyNames);
        Assert.Contains("buildingId", propertyNames);
        Assert.Contains("steps", propertyNames);
        Assert.Contains("availableModules", propertyNames);
        Assert.Contains("diagnostics", propertyNames);
        Assert.Contains("metadata", propertyNames);
    }

    private static void AssertActionParameterType(
        IReadOnlyDictionary<string, MethodInfo> methods,
        string actionName,
        Type expectedType)
    {
        var parameterType = methods[actionName]
            .GetParameters()
            .First()
            .ParameterType;

        Assert.Equal(expectedType, parameterType);
    }
}
