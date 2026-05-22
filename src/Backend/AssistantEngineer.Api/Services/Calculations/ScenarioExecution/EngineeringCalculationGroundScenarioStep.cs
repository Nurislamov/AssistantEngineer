using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationGroundScenarioStep : IEngineeringCalculationGroundScenarioStep
{
    public ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.State);

        if (request.State.GroundSettings.GroundBoundaryCount <= 0)
        {
            return ScenarioModuleExecution.Skip(
                "No ground boundaries are configured.",
                "Configure ground boundaries to execute this module.");
        }

        return ScenarioModuleExecution.Skip(
            "Structured ground boundary geometry and climate inputs are not available in workflow state foundation payload.",
            "Provide detailed ground inputs to execute this module.");
    }
}