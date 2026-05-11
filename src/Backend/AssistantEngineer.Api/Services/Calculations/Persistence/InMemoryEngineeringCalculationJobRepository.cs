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
        Upsert(job);
        return Task.FromResult(job);
    }

    public Task<EngineeringCalculationJobRecordDto> UpdateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken)
    {
        Upsert(job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListQueuedAsync(
        int maxCount,
        CancellationToken cancellationToken)
    {
        var take = Math.Max(1, maxCount);
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

    public Task<EngineeringCalculationJobRecordDto?> TryClaimQueuedJobAsync(
        string jobId,
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            if (!_store.JobsById.TryGetValue(jobId, out var job))
            {
                return Task.FromResult<EngineeringCalculationJobRecordDto?>(null);
            }

            if (job.CancellationRequested ||
                job.Status is not (EngineeringCalculationJobStatus.Queued or EngineeringCalculationJobStatus.RetryScheduled))
            {
                return Task.FromResult<EngineeringCalculationJobRecordDto?>(null);
            }

            var claimedAt = DateTimeOffset.UtcNow;
            var claimed = job with
            {
                Status = EngineeringCalculationJobStatus.Running,
                ProgressPercent = Math.Max(job.ProgressPercent, 25),
                CurrentStep = "Running",
                StartedAtUtc = job.StartedAtUtc ?? claimedAt,
                UpdatedAtUtc = claimedAt,
                ClaimedByWorkerId = workerId,
                ClaimedAtUtc = claimedAt,
                LeaseExpiresAtUtc = claimedAt.Add(leaseDuration)
            };

            if (_store.JobsById.TryUpdate(jobId, claimed, job))
            {
                return Task.FromResult<EngineeringCalculationJobRecordDto?>(claimed);
            }
        }
    }

    public Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        _store.JobsById.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var jobs = _store.JobsById.Values
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.JobId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EngineeringCalculationJobRecordDto>>(jobs);
    }

    private void Upsert(EngineeringCalculationJobRecordDto job)
    {
        _store.JobsById.AddOrUpdate(job.JobId, job, (_, _) => job);
    }
}
