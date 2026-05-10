using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class InMemoryEngineeringCalculationArtifactRepository : IEngineeringCalculationArtifactRepository
{
    private readonly EngineeringWorkflowMemoryStore _store;

    public InMemoryEngineeringCalculationArtifactRepository(EngineeringWorkflowMemoryStore store)
    {
        _store = store;
    }

    public Task<EngineeringCalculationArtifactRecordDto> SaveAsync(
        EngineeringCalculationArtifactRecordDto artifact,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.ArtifactsById[artifact.ArtifactId] = artifact;

            if (!_store.ArtifactIdsByScenarioId.TryGetValue(artifact.ScenarioId, out var ids))
            {
                ids = [];
                _store.ArtifactIdsByScenarioId[artifact.ScenarioId] = ids;
            }

            if (!ids.Contains(artifact.ArtifactId, StringComparer.Ordinal))
            {
                ids.Add(artifact.ArtifactId);
            }

            return Task.FromResult(artifact);
        }
    }

    public Task<EngineeringCalculationArtifactRecordDto?> GetByScenarioAndKindAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.ArtifactIdsByScenarioId.TryGetValue(scenarioId, out var ids) || ids.Count == 0)
            {
                return Task.FromResult<EngineeringCalculationArtifactRecordDto?>(null);
            }

            var artifact = ids
                .Select(id => _store.ArtifactsById[id])
                .Where(item => item.ArtifactKind == artifactKind)
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.ArtifactId, StringComparer.Ordinal)
                .FirstOrDefault();

            return Task.FromResult(artifact);
        }
    }

    public Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.ArtifactIdsByScenarioId.TryGetValue(scenarioId, out var ids))
            {
                return Task.FromResult<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>([]);
            }

            var artifacts = ids
                .Select(id => _store.ArtifactsById[id])
                .OrderBy(item => item.ArtifactKind.ToString(), StringComparer.Ordinal)
                .ThenByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.ArtifactId, StringComparer.Ordinal)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>(artifacts);
        }
    }
}
