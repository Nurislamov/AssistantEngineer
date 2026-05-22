using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationWeatherSolarScenarioStep
{
    ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request);
}