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
        _store.HistoryById.AddOrUpdate(historyEntry.EventId, historyEntry, (_, _) => historyEntry);
        return Task.FromResult(historyEntry);
    }

    public Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var items = _store.HistoryById.Values
            .Where(item => item.ScenarioId.Equals(scenarioId, StringComparison.Ordinal))
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.EventId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EngineeringScenarioHistoryEntryDto>>(items);
    }

    public Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var items = _store.HistoryById.Values
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.EventId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EngineeringScenarioHistoryEntryDto>>(items);
    }
}
