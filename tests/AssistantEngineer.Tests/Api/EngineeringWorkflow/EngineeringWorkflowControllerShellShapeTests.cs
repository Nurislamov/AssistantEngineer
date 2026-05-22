using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Api.Services.Calculations.Composition;

namespace AssistantEngineer.Tests.Api.EngineeringWorkflow;

public sealed class EngineeringWorkflowControllerShellShapeTests
{
    [Fact]
    public void ControllerConstructorDependsOnActionServiceAndKeepsApiAdapterBoundary()
    {
        var constructor = Assert.Single(typeof(EngineeringWorkflowController).GetConstructors());
        var dependencies = constructor.GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IEngineeringWorkflowControllerActionService), dependencies);
    }

    [Fact]
    public void MainControllerPartialUsesExtractedActionServiceForValidateAndPrepare()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.ApiProjectPath,
            "Controllers",
            "Calculations",
            "EngineeringWorkflowController.cs"));

        Assert.Contains("_actionService.ValidateAsync", source, StringComparison.Ordinal);
        Assert.Contains("_actionService.PrepareCalculationAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyWorkflowServiceRegistrationPathIsNotPresent()
    {
        var legacyPath = Path.Combine(
            TestPaths.ApiProjectPath,
            "Services",
            "Calculations",
            "Workflow",
            "EngineeringWorkflowServiceRegistration.cs");

        Assert.False(File.Exists(legacyPath));
    }
}

