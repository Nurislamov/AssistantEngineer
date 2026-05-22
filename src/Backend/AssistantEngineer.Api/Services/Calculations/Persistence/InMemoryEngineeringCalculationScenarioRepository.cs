using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

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
        _store.ScenariosById.AddOrUpdate(scenario.ScenarioId, scenario, (_, _) => scenario);
        return Task.FromResult(scenario);
    }

    public Task<EngineeringCalculationScenarioRecordDto> UpdateAsync(
        EngineeringCalculationScenarioRecordDto scenario,
        CancellationToken cancellationToken)
    {
        _store.ScenariosById.AddOrUpdate(scenario.ScenarioId, scenario, (_, _) => scenario);
        return Task.FromResult(scenario);
    }

    public Task<EngineeringCalculationScenarioRecordDto?> GetByIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        _store.ScenariosById.TryGetValue(scenarioId, out var scenario);
        return Task.FromResult(scenario);
    }

    public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var scenarios = _store.ScenariosById.Values
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.ScenarioId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>(scenarios);
    }

    public Task<EngineeringCalculationScenarioRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var scenario = _store.ScenariosById.Values
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.ScenarioId, StringComparer.Ordinal)
            .FirstOrDefault();

        return Task.FromResult(scenario);
    }
}
