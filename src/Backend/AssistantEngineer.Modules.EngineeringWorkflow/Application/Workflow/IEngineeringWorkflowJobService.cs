using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public interface IEngineeringWorkflowJobService
{
    Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(
        EngineeringCalculationJobRequestDto request,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobResultDto?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken);
}
