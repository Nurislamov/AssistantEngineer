using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationWeatherSolarScenarioStep
{
    ScenarioModuleExecution Execute(EngineeringCalculationScenarioRequestDto request);
}