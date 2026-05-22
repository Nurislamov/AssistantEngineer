namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public interface IEngineeringWorkflowInputSnapshotBuilder
{
    Task<EngineeringWorkflowInputSnapshot> BuildAsync(
        int projectId,
        int? buildingId,
        int weatherYear,
        CancellationToken cancellationToken);
}