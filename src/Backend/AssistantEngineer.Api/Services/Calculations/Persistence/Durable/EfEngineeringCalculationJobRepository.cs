using AssistantEngineer.Api.Contracts.Calculations;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EfEngineeringCalculationJobRepository : IEngineeringCalculationJobRepository
{
    private readonly EngineeringWorkflowPersistenceDbContext _context;

    public EfEngineeringCalculationJobRepository(EngineeringWorkflowPersistenceDbContext context)
    {
        _context = context;
    }

    public async Task<EngineeringCalculationJobRecordDto> CreateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken)
    {
        var entity = ToEntity(job);
        _context.Jobs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EngineeringCalculationJobRecordDto> UpdateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Jobs.SingleOrDefaultAsync(item => item.Id == job.JobId, cancellationToken);
        if (entity is null)
        {
            entity = ToEntity(job);
            _context.Jobs.Add(entity);
        }
        else
        {
            entity.ProjectId = job.ProjectId;
            entity.ScenarioId = job.ScenarioId;
            entity.Status = job.Status.ToString();
            entity.ExecutionMode = job.ExecutionMode.ToString();
            entity.RequestJson = job.RequestJson;
            entity.ResultSummaryJson = job.ResultSummaryJson;
            entity.DiagnosticsJson = job.DiagnosticsJson;
            entity.ProgressPercent = job.ProgressPercent;
            entity.CurrentStep = job.CurrentStep;
            entity.CreatedAtUtc = job.CreatedAtUtc;
            entity.QueuedAtUtc = job.QueuedAtUtc;
            entity.StartedAtUtc = job.StartedAtUtc;
            entity.CompletedAtUtc = job.CompletedAtUtc;
            entity.UpdatedAtUtc = job.UpdatedAtUtc;
            entity.DurationMs = job.DurationMilliseconds;
            entity.RetryCount = job.RetryCount;
            entity.CancellationRequested = job.CancellationRequested;
            entity.ClaimedByWorkerId = job.ClaimedByWorkerId;
            entity.ClaimedAtUtc = job.ClaimedAtUtc;
            entity.LeaseExpiresAtUtc = job.LeaseExpiresAtUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListQueuedAsync(
        int maxCount,
        CancellationToken cancellationToken)
    {
        var take = Math.Max(1, maxCount);
        var queued = EngineeringCalculationJobStatus.Queued.ToString();
        var retryScheduled = EngineeringCalculationJobStatus.RetryScheduled.ToString();

        var entities = await _context.Jobs
            .AsNoTracking()
            .Where(item =>
                !item.CancellationRequested &&
                (item.Status == queued || item.Status == retryScheduled))
            .OrderBy(item => item.QueuedAtUtc ?? item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .Take(take)
            .ToArrayAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    public async Task<EngineeringCalculationJobRecordDto?> TryClaimQueuedJobAsync(
        string jobId,
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var claimedAt = DateTimeOffset.UtcNow;
        var leaseExpiresAt = claimedAt.Add(leaseDuration);
        var running = EngineeringCalculationJobStatus.Running.ToString();
        var queued = EngineeringCalculationJobStatus.Queued.ToString();
        var retryScheduled = EngineeringCalculationJobStatus.RetryScheduled.ToString();

        var rowsAffected = await _context.Jobs
            .Where(item =>
                item.Id == jobId &&
                !item.CancellationRequested &&
                (item.Status == queued || item.Status == retryScheduled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, running)
                .SetProperty(item => item.ProgressPercent, item => item.ProgressPercent < 25 ? 25 : item.ProgressPercent)
                .SetProperty(item => item.CurrentStep, "Running")
                .SetProperty(item => item.StartedAtUtc, item => item.StartedAtUtc ?? claimedAt)
                .SetProperty(item => item.UpdatedAtUtc, claimedAt)
                .SetProperty(item => item.ClaimedByWorkerId, workerId)
                .SetProperty(item => item.ClaimedAtUtc, claimedAt)
                .SetProperty(item => item.LeaseExpiresAtUtc, leaseExpiresAt),
                cancellationToken);

        if (rowsAffected != 1)
        {
            return null;
        }

        var claimed = await _context.Jobs
            .AsNoTracking()
            .SingleAsync(item => item.Id == jobId, cancellationToken);

        return Map(claimed);
    }

    public async Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Jobs
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var entities = (await _context.Jobs
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .ToArrayAsync(cancellationToken))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();

        return entities.Select(Map).ToArray();
    }

    private static EngineeringCalculationJobEntity ToEntity(EngineeringCalculationJobRecordDto source)
    {
        return new EngineeringCalculationJobEntity
        {
            Id = source.JobId,
            ProjectId = source.ProjectId,
            ScenarioId = source.ScenarioId,
            Status = source.Status.ToString(),
            ExecutionMode = source.ExecutionMode.ToString(),
            RequestJson = source.RequestJson,
            ResultSummaryJson = source.ResultSummaryJson,
            DiagnosticsJson = source.DiagnosticsJson,
            ProgressPercent = source.ProgressPercent,
            CurrentStep = source.CurrentStep,
            CreatedAtUtc = source.CreatedAtUtc,
            QueuedAtUtc = source.QueuedAtUtc,
            StartedAtUtc = source.StartedAtUtc,
            CompletedAtUtc = source.CompletedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            DurationMs = source.DurationMilliseconds,
            RetryCount = source.RetryCount,
            CancellationRequested = source.CancellationRequested,
            ClaimedByWorkerId = source.ClaimedByWorkerId,
            ClaimedAtUtc = source.ClaimedAtUtc,
            LeaseExpiresAtUtc = source.LeaseExpiresAtUtc
        };
    }

    private static EngineeringCalculationJobRecordDto Map(EngineeringCalculationJobEntity entity)
    {
        var status = Enum.TryParse<EngineeringCalculationJobStatus>(entity.Status, true, out var parsedStatus)
            ? parsedStatus
            : EngineeringCalculationJobStatus.NotSupported;
        var mode = Enum.TryParse<EngineeringCalculationJobExecutionMode>(entity.ExecutionMode, true, out var parsedMode)
            ? parsedMode
            : EngineeringCalculationJobExecutionMode.Synchronous;

        return new EngineeringCalculationJobRecordDto(
            JobId: entity.Id,
            ProjectId: entity.ProjectId,
            ScenarioId: entity.ScenarioId,
            Status: status,
            ExecutionMode: mode,
            RequestJson: entity.RequestJson,
            ResultSummaryJson: entity.ResultSummaryJson,
            DiagnosticsJson: entity.DiagnosticsJson,
            ProgressPercent: entity.ProgressPercent,
            CurrentStep: entity.CurrentStep,
            CreatedAtUtc: entity.CreatedAtUtc,
            QueuedAtUtc: entity.QueuedAtUtc,
            StartedAtUtc: entity.StartedAtUtc,
            CompletedAtUtc: entity.CompletedAtUtc,
            UpdatedAtUtc: entity.UpdatedAtUtc,
            DurationMilliseconds: entity.DurationMs,
            RetryCount: entity.RetryCount,
            CancellationRequested: entity.CancellationRequested,
            ClaimedByWorkerId: entity.ClaimedByWorkerId,
            ClaimedAtUtc: entity.ClaimedAtUtc,
            LeaseExpiresAtUtc: entity.LeaseExpiresAtUtc);
    }
}
