using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class InMemoryEngineeringWorkflowStateRepository : IEngineeringWorkflowStateRepository
{
    private readonly EngineeringWorkflowMemoryStore _store;

    public InMemoryEngineeringWorkflowStateRepository(EngineeringWorkflowMemoryStore store)
    {
        _store = store;
    }

    public Task<EngineeringWorkflowStateRecordDto> SaveAsync(
        EngineeringWorkflowStateRecordDto state,
        CancellationToken cancellationToken)
    {
        _store.WorkflowStatesById.AddOrUpdate(state.WorkflowStateId, state, (_, _) => state);
        return Task.FromResult(state);
    }

    public Task<EngineeringWorkflowStateRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var latest = _store.WorkflowStatesById.Values
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.Version)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .ThenBy(item => item.WorkflowStateId, StringComparer.Ordinal)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<EngineeringWorkflowStateRecordDto?> GetByIdAsync(
        string workflowStateId,
        CancellationToken cancellationToken)
    {
        _store.WorkflowStatesById.TryGetValue(workflowStateId, out var state);
        return Task.FromResult(state);
    }

    public Task<IReadOnlyList<EngineeringWorkflowStateRecordDto>> ListVersionsByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var states = _store.WorkflowStatesById.Values
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.Version)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .ThenBy(item => item.WorkflowStateId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EngineeringWorkflowStateRecordDto>>(states);
    }
}
