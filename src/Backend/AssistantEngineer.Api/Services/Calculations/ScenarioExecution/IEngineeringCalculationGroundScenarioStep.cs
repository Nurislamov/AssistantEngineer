using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationGroundScenarioStep
{
    ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request);
}