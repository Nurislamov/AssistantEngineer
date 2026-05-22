using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationVentilationScenarioStep : IEngineeringCalculationVentilationScenarioStep
{
    public ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.State);

        if (request.State.VentilationSettings.OpeningCount <= 0)
        {
            return ScenarioModuleExecution.Skip(
                "No natural ventilation openings are configured.",
                "Configure natural ventilation openings to execute this module.");
        }

        return ScenarioModuleExecution.Skip(
            "Structured natural ventilation hourly input is not available in workflow state foundation payload.",
            "Provide detailed ventilation opening geometry, control rules and hourly environments.");
    }
}