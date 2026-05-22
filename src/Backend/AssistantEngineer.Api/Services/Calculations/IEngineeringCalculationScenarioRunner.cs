using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationScenarioRunner
{
    Task<EngineeringCalculationScenarioResultDto> RunAsync(
        EngineeringCalculationScenarioRequestDto request,
        CancellationToken cancellationToken);
}
