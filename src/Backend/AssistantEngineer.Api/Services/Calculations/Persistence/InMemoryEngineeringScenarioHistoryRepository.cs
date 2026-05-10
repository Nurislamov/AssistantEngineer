using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class InMemoryEngineeringScenarioHistoryRepository : IEngineeringScenarioHistoryRepository
{
    private readonly EngineeringWorkflowMemoryStore _store;

    public InMemoryEngineeringScenarioHistoryRepository(EngineeringWorkflowMemoryStore store)
    {
        _store = store;
    }

    public Task<EngineeringScenarioHistoryEntryDto> AppendAsync(
        EngineeringScenarioHistoryEntryDto historyEntry,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.HistoryById[historyEntry.EventId] = historyEntry;

            if (!_store.HistoryIdsByScenarioId.TryGetValue(historyEntry.ScenarioId, out var scenarioIds))
            {
                scenarioIds = [];
                _store.HistoryIdsByScenarioId[historyEntry.ScenarioId] = scenarioIds;
            }

            if (!scenarioIds.Contains(historyEntry.EventId, StringComparer.Ordinal))
            {
                scenarioIds.Add(historyEntry.EventId);
            }

            if (!_store.HistoryIdsByProjectId.TryGetValue(historyEntry.ProjectId, out var projectIds))
            {
                projectIds = [];
                _store.HistoryIdsByProjectId[historyEntry.ProjectId] = projectIds;
            }

            if (!projectIds.Contains(historyEntry.EventId, StringComparer.Ordinal))
            {
                projectIds.Add(historyEntry.EventId);
            }

            return Task.FromResult(historyEntry);
        }
    }

    public Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.HistoryIdsByScenarioId.TryGetValue(scenarioId, out var ids))
            {
                return Task.FromResult<IReadOnlyList<EngineeringScenarioHistoryEntryDto>>([]);
            }

            var items = ids
                .Select(id => _store.HistoryById[id])
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.EventId, StringComparer.Ordinal)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringScenarioHistoryEntryDto>>(items);
        }
    }

    public Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.HistoryIdsByProjectId.TryGetValue(projectId, out var ids))
            {
                return Task.FromResult<IReadOnlyList<EngineeringScenarioHistoryEntryDto>>([]);
            }

            var items = ids
                .Select(id => _store.HistoryById[id])
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.EventId, StringComparer.Ordinal)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringScenarioHistoryEntryDto>>(items);
        }
    }
}
