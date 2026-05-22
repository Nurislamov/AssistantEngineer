using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public interface IEngineeringWorkflowStateBuilder
{
    Task<EngineeringWorkflowStateDto> BuildWorkflowStateAsync(
        int projectId,
        int? buildingId,
        CancellationToken cancellationToken);

    EngineeringWorkflowStateDto BuildInfrastructureFallbackState(
        int projectId,
        int? buildingId,
        string errorMessage);
}
