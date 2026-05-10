using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class InMemoryEngineeringCalculationJobRepository : IEngineeringCalculationJobRepository
{
    private readonly EngineeringWorkflowMemoryStore _store;

    public InMemoryEngineeringCalculationJobRepository(EngineeringWorkflowMemoryStore store)
    {
        _store = store;
    }

    public Task<EngineeringCalculationJobRecordDto> CreateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            Upsert(job);
            return Task.FromResult(job);
        }
    }

    public Task<EngineeringCalculationJobRecordDto> UpdateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            Upsert(job);
            return Task.FromResult(job);
        }
    }

    public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListQueuedAsync(
        int maxCount,
        CancellationToken cancellationToken)
    {
        var take = Math.Max(1, maxCount);
        lock (_store.SyncRoot)
        {
            var jobs = _store.JobsById.Values
                .Where(item =>
                    !item.CancellationRequested &&
                    item.Status is EngineeringCalculationJobStatus.Queued or EngineeringCalculationJobStatus.RetryScheduled)
                .OrderBy(item => item.QueuedAtUtc ?? item.CreatedAtUtc)
                .ThenBy(item => item.JobId, StringComparer.Ordinal)
                .Take(take)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobRecordDto>>(jobs);
        }
    }

    public Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            _store.JobsById.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }
    }

    public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.JobIdsByProjectId.TryGetValue(projectId, out var ids))
            {
                return Task.FromResult<IReadOnlyList<EngineeringCalculationJobRecordDto>>([]);
            }

            var jobs = ids
                .Select(id => _store.JobsById[id])
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.JobId, StringComparer.Ordinal)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobRecordDto>>(jobs);
        }
    }

    private void Upsert(EngineeringCalculationJobRecordDto job)
    {
        _store.JobsById[job.JobId] = job;
        if (!_store.JobIdsByProjectId.TryGetValue(job.ProjectId, out var ids))
        {
            ids = [];
            _store.JobIdsByProjectId[job.ProjectId] = ids;
        }

        if (!ids.Contains(job.JobId, StringComparer.Ordinal))
        {
            ids.Add(job.JobId);
        }
    }
}