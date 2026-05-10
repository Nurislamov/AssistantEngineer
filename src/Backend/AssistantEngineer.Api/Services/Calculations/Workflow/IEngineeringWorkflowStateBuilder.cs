using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Workflow;

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
