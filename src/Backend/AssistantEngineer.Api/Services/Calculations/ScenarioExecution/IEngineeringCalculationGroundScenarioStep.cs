using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationGroundScenarioStep
{
    ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request);
}