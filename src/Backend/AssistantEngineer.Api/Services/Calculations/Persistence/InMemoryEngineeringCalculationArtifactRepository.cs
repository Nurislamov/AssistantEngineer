using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

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
        _store.ArtifactsById.AddOrUpdate(artifact.ArtifactId, artifact, (_, _) => artifact);
        return Task.FromResult(artifact);
    }

    public Task<EngineeringCalculationArtifactRecordDto?> GetByScenarioAndKindAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        CancellationToken cancellationToken)
    {
        var artifact = _store.ArtifactsById.Values
            .Where(item => item.ScenarioId.Equals(scenarioId, StringComparison.Ordinal) && item.ArtifactKind == artifactKind)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.ArtifactId, StringComparer.Ordinal)
            .FirstOrDefault();

        return Task.FromResult(artifact);
    }

    public Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var artifacts = _store.ArtifactsById.Values
            .Where(item => item.ScenarioId.Equals(scenarioId, StringComparison.Ordinal))
            .OrderBy(item => item.ArtifactKind.ToString(), StringComparer.Ordinal)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.ArtifactId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>(artifacts);
    }
}
