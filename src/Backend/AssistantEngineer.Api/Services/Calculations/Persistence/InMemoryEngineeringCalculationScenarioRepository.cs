using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class InMemoryEngineeringCalculationScenarioRepository : IEngineeringCalculationScenarioRepository
{
    private readonly EngineeringWorkflowMemoryStore _store;

    public InMemoryEngineeringCalculationScenarioRepository(EngineeringWorkflowMemoryStore store)
    {
        _store = store;
    }

    public Task<EngineeringCalculationScenarioRecordDto> CreateAsync(
        EngineeringCalculationScenarioRecordDto scenario,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.ScenariosById[scenario.ScenarioId] = scenario;

            if (!_store.ScenarioIdsByProjectId.TryGetValue(scenario.ProjectId, out var ids))
            {
                ids = [];
                _store.ScenarioIdsByProjectId[scenario.ProjectId] = ids;
            }

            if (!ids.Contains(scenario.ScenarioId, StringComparer.Ordinal))
            {
                ids.Add(scenario.ScenarioId);
            }

            return Task.FromResult(scenario);
        }
    }

    public Task<EngineeringCalculationScenarioRecordDto> UpdateAsync(
        EngineeringCalculationScenarioRecordDto scenario,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.ScenariosById[scenario.ScenarioId] = scenario;

            if (!_store.ScenarioIdsByProjectId.TryGetValue(scenario.ProjectId, out var ids))
            {
                ids = [];
                _store.ScenarioIdsByProjectId[scenario.ProjectId] = ids;
            }

            if (!ids.Contains(scenario.ScenarioId, StringComparer.Ordinal))
            {
                ids.Add(scenario.ScenarioId);
            }

            return Task.FromResult(scenario);
        }
    }

    public Task<EngineeringCalculationScenarioRecordDto?> GetByIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.ScenariosById.TryGetValue(scenarioId, out var scenario);
            return Task.FromResult(scenario);
        }
    }

    public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.ScenarioIdsByProjectId.TryGetValue(projectId, out var ids))
            {
                return Task.FromResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>([]);
            }

            var scenarios = ids
                .Select(id => _store.ScenariosById[id])
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.ScenarioId, StringComparer.Ordinal)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>(scenarios);
        }
    }

    public Task<EngineeringCalculationScenarioRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.ScenarioIdsByProjectId.TryGetValue(projectId, out var ids) || ids.Count == 0)
            {
                return Task.FromResult<EngineeringCalculationScenarioRecordDto?>(null);
            }

            var scenario = ids
                .Select(id => _store.ScenariosById[id])
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.ScenarioId, StringComparer.Ordinal)
                .FirstOrDefault();

            return Task.FromResult(scenario);
        }
    }
}
