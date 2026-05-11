using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class InMemoryEngineeringProjectRepository : IEngineeringProjectRepository
{
    private readonly EngineeringWorkflowMemoryStore _store;

    public InMemoryEngineeringProjectRepository(EngineeringWorkflowMemoryStore store)
    {
        _store = store;
    }

    public Task<EngineeringProjectRecordDto> UpsertAsync(
        EngineeringProjectRecordDto project,
        CancellationToken cancellationToken)
    {
        _store.Projects.AddOrUpdate(project.ProjectId, project, (_, _) => project);
        return Task.FromResult(project);
    }

    public Task<EngineeringProjectRecordDto?> GetByIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        _store.Projects.TryGetValue(projectId, out var project);
        return Task.FromResult(project);
    }

    public Task<IReadOnlyList<EngineeringProjectRecordDto>> ListAsync(
        CancellationToken cancellationToken)
    {
        var items = _store.Projects.Values
            .OrderBy(item => item.ProjectId)
            .ToArray();
        return Task.FromResult<IReadOnlyList<EngineeringProjectRecordDto>>(items);
    }

    public Task<EngineeringProjectRecordDto?> UpdateMetadataAsync(
        int projectId,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        if (!_store.Projects.TryGetValue(projectId, out var existing))
        {
            return Task.FromResult<EngineeringProjectRecordDto?>(null);
        }

        var updated = existing with
        {
            MetadataJson = metadata
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _store.Projects.AddOrUpdate(projectId, updated, (_, _) => updated);
        return Task.FromResult<EngineeringProjectRecordDto?>(updated);
    }
}
