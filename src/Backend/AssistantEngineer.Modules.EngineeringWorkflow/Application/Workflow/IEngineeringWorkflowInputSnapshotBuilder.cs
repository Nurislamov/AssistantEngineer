namespace AssistantEngineer.Api.Services.Calculations.Workflow;

public interface IEngineeringWorkflowInputSnapshotBuilder
{
    Task<EngineeringWorkflowInputSnapshot> BuildAsync(
        int projectId,
        int? buildingId,
        int weatherYear,
        CancellationToken cancellationToken);
}