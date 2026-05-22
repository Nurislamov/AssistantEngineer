using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringWorkflowStateRepository
{
    Task<EngineeringWorkflowStateRecordDto> SaveAsync(
        EngineeringWorkflowStateRecordDto state,
        CancellationToken cancellationToken);

    Task<EngineeringWorkflowStateRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<EngineeringWorkflowStateRecordDto?> GetByIdAsync(
        string workflowStateId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringWorkflowStateRecordDto>> ListVersionsByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);
}
