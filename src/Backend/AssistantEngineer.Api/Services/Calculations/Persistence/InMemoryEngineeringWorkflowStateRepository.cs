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
        lock (_store.SyncRoot)
        {
            _store.WorkflowStatesById[state.WorkflowStateId] = state;

            if (!_store.WorkflowStateIdsByProjectId.TryGetValue(state.ProjectId, out var ids))
            {
                ids = [];
                _store.WorkflowStateIdsByProjectId[state.ProjectId] = ids;
            }

            if (!ids.Contains(state.WorkflowStateId, StringComparer.Ordinal))
            {
                ids.Add(state.WorkflowStateId);
            }

            return Task.FromResult(state);
        }
    }

    public Task<EngineeringWorkflowStateRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.WorkflowStateIdsByProjectId.TryGetValue(projectId, out var ids) || ids.Count == 0)
            {
                return Task.FromResult<EngineeringWorkflowStateRecordDto?>(null);
            }

            var latest = ids
                .Select(id => _store.WorkflowStatesById[id])
                .OrderByDescending(item => item.Version)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefault();

            return Task.FromResult(latest);
        }
    }

    public Task<EngineeringWorkflowStateRecordDto?> GetByIdAsync(
        string workflowStateId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.WorkflowStatesById.TryGetValue(workflowStateId, out var state);
            return Task.FromResult(state);
        }
    }

    public Task<IReadOnlyList<EngineeringWorkflowStateRecordDto>> ListVersionsByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.WorkflowStateIdsByProjectId.TryGetValue(projectId, out var ids))
            {
                return Task.FromResult<IReadOnlyList<EngineeringWorkflowStateRecordDto>>([]);
            }

            var states = ids
                .Select(id => _store.WorkflowStatesById[id])
                .OrderByDescending(item => item.Version)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringWorkflowStateRecordDto>>(states);
        }
    }
}
