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
        lock (_store.SyncRoot)
        {
            _store.Projects[project.ProjectId] = project;
            return Task.FromResult(project);
        }
    }

    public Task<EngineeringProjectRecordDto?> GetByIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.Projects.TryGetValue(projectId, out var project);
            return Task.FromResult(project);
        }
    }

    public Task<IReadOnlyList<EngineeringProjectRecordDto>> ListAsync(
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            var items = _store.Projects.Values
                .OrderBy(item => item.ProjectId)
                .ToArray();
            return Task.FromResult<IReadOnlyList<EngineeringProjectRecordDto>>(items);
        }
    }

    public Task<EngineeringProjectRecordDto?> UpdateMetadataAsync(
        int projectId,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
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

            _store.Projects[projectId] = updated;
            return Task.FromResult<EngineeringProjectRecordDto?>(updated);
        }
    }
}
