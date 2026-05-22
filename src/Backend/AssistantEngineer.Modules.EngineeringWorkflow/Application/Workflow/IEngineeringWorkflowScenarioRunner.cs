using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public interface IEngineeringWorkflowScenarioRunner
{
    Task<EngineeringCalculationScenarioResultDto> RunAsync(
        EngineeringCalculationScenarioRequestDto request,
        CancellationToken cancellationToken);
}
