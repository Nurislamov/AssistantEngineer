using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationVentilationScenarioStep
{
    ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request);
}