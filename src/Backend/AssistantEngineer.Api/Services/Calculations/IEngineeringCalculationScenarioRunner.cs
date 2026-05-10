using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationScenarioRunner
{
    Task<EngineeringCalculationScenarioResultDto> RunAsync(
        EngineeringCalculationScenarioRequestDto request,
        CancellationToken cancellationToken);
}
