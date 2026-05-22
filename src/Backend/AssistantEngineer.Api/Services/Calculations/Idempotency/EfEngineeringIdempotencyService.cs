using System.Text;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Idempotency;

public sealed class EfEngineeringIdempotencyService : IEngineeringIdempotencyService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EngineeringIdempotencyOptions _options;
    private readonly ILogger<EfEngineeringIdempotencyService> _logger;

    public EfEngineeringIdempotencyService(
        IServiceScopeFactory scopeFactory,
        IOptions<EngineeringIdempotencyOptions> options,
        ILogger<EfEngineeringIdempotencyService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EngineeringIdempotencyEvaluationResult> EvaluateAsync(
        string? idempotencyKey,
        string scope,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Enabled || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new EngineeringIdempotencyEvaluationResult(EngineeringIdempotencyEvaluationKind.Bypass);
        }

        var normalizedKey = NormalizeKey(idempotencyKey);
        var now = DateTimeOffset.UtcNow;

        using var serviceScope = _scopeFactory.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>();

        await CleanupExpiredAsync(dbContext, now, cancellationToken);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = await dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Scope == scope && item.IdempotencyKey == normalizedKey,
                    cancellationToken);

            if (current is null)
            {
                var ttlMinutes = Math.Max(1, _options.TtlMinutes);
                var created = new EngineeringWorkflowIdempotencyRecordEntity
                {
                    Scope = scope,
                    IdempotencyKey = normalizedKey,
                    RequestFingerprint = requestFingerprint,
                    Status = EntryStatusPending,
                    ResponseJson = null,
                    ResponseReferenceId = null,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    ExpiresAtUtc = now.AddMinutes(ttlMinutes),
                    CompletedAtUtc = null,
                    FailureReason = null
                };

                dbContext.IdempotencyRecords.Add(created);
                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await EnforceEntryLimitAsync(dbContext, cancellationToken);
                    _logger.LogInformation(
                        "Engineering idempotency key accepted in durable store. Scope={Scope}, Key={IdempotencyKey}",
                        scope,
                        normalizedKey);
                    return new EngineeringIdempotencyEvaluationResult(EngineeringIdempotencyEvaluationKind.Proceed);
                }
                catch (DbUpdateException)
                {
                    dbContext.ChangeTracker.Clear();
                    continue;
                }
            }

            if (!string.Equals(current.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Engineering idempotency conflict detected in durable store. Scope={Scope}, Key={IdempotencyKey}",
                    scope,
                    normalizedKey);
                return new EngineeringIdempotencyEvaluationResult(
                    EngineeringIdempotencyEvaluationKind.Conflict,
                    ConflictCode: "ENGINEERING_IDEMPOTENCY_CONFLICT",
                    ConflictMessage: "Idempotency key already exists for this scope with a different request payload.");
            }

            if (string.Equals(current.Status, EntryStatusCompleted, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Engineering idempotency replay hit in durable store. Scope={Scope}, Key={IdempotencyKey}",
                    scope,
                    normalizedKey);
                return new EngineeringIdempotencyEvaluationResult(
                    EngineeringIdempotencyEvaluationKind.Replay,
                    ReplayPayload: new EngineeringIdempotencyReplayPayload(
                        current.ResponseJson,
                        current.ResponseReferenceId));
            }

            _logger.LogWarning(
                "Engineering idempotency key is already in progress in durable store. Scope={Scope}, Key={IdempotencyKey}",
                scope,
                normalizedKey);
            return new EngineeringIdempotencyEvaluationResult(
                EngineeringIdempotencyEvaluationKind.Conflict,
                ConflictCode: "ENGINEERING_IDEMPOTENCY_IN_PROGRESS",
                ConflictMessage: "Idempotency key is already being processed for this scope.");
        }
    }

    public async Task RecordSuccessAsync(
        string idempotencyKey,
        string scope,
        string requestFingerprint,
        string? responseJson,
        string? responseReferenceId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Enabled || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return;
        }

        var normalizedKey = NormalizeKey(idempotencyKey);
        var now = DateTimeOffset.UtcNow;
        var boundedResponse = BoundResponsePayload(responseJson);
        if (boundedResponse.WasTruncated)
        {
            _logger.LogWarning(
                "Engineering idempotency response payload exceeded cache cap in durable store and was not stored inline. Scope={Scope}, Key={IdempotencyKey}, MaxBytes={MaxBytes}",
                scope,
                normalizedKey,
                _options.MaxCachedResponseBytes);
        }

        using var serviceScope = _scopeFactory.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>();
        var ttlMinutes = Math.Max(1, _options.TtlMinutes);

        var updatedRows = await dbContext.IdempotencyRecords
            .Where(item =>
                item.Scope == scope &&
                item.IdempotencyKey == normalizedKey &&
                item.RequestFingerprint == requestFingerprint)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, EntryStatusCompleted)
                .SetProperty(item => item.ResponseJson, boundedResponse.Content)
                .SetProperty(item => item.ResponseReferenceId, responseReferenceId)
                .SetProperty(item => item.UpdatedAtUtc, now)
                .SetProperty(item => item.ExpiresAtUtc, now.AddMinutes(ttlMinutes))
                .SetProperty(item => item.CompletedAtUtc, now)
                .SetProperty(item => item.FailureReason, (string?)null),
                cancellationToken);

        if (updatedRows == 0)
        {
            var created = new EngineeringWorkflowIdempotencyRecordEntity
            {
                Scope = scope,
                IdempotencyKey = normalizedKey,
                RequestFingerprint = requestFingerprint,
                Status = EntryStatusCompleted,
                ResponseJson = boundedResponse.Content,
                ResponseReferenceId = responseReferenceId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(ttlMinutes),
                CompletedAtUtc = now,
                FailureReason = null
            };

            dbContext.IdempotencyRecords.Add(created);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await dbContext.IdempotencyRecords
                    .Where(item =>
                        item.Scope == scope &&
                        item.IdempotencyKey == normalizedKey &&
                        item.RequestFingerprint == requestFingerprint)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Status, EntryStatusCompleted)
                        .SetProperty(item => item.ResponseJson, boundedResponse.Content)
                        .SetProperty(item => item.ResponseReferenceId, responseReferenceId)
                        .SetProperty(item => item.UpdatedAtUtc, now)
                        .SetProperty(item => item.ExpiresAtUtc, now.AddMinutes(ttlMinutes))
                        .SetProperty(item => item.CompletedAtUtc, now)
                        .SetProperty(item => item.FailureReason, (string?)null),
                        cancellationToken);
            }
        }

        await EnforceEntryLimitAsync(dbContext, cancellationToken);

        _logger.LogInformation(
            "Engineering idempotency response persisted in durable store. Scope={Scope}, Key={IdempotencyKey}, HasInlineResponse={HasInlineResponse}, HasReference={HasReference}",
            scope,
            normalizedKey,
            !string.IsNullOrWhiteSpace(boundedResponse.Content),
            !string.IsNullOrWhiteSpace(responseReferenceId));
    }

    private async Task CleanupExpiredAsync(
        EngineeringWorkflowPersistenceDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var all = await dbContext.IdempotencyRecords
            .ToArrayAsync(cancellationToken);
        var expired = all
            .Where(item => item.ExpiresAtUtc <= now)
            .ToArray();
        if (expired.Length == 0)
        {
            return;
        }

        dbContext.IdempotencyRecords.RemoveRange(expired);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnforceEntryLimitAsync(
        EngineeringWorkflowPersistenceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var maxEntries = Math.Max(1, _options.MaxEntries);
        var count = await dbContext.IdempotencyRecords.CountAsync(cancellationToken);
        var overflow = count - maxEntries;
        if (overflow <= 0)
        {
            return;
        }

        var toRemoveIds = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .Take(overflow)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);

        if (toRemoveIds.Length == 0)
        {
            return;
        }

        var toRemove = await dbContext.IdempotencyRecords
            .Where(item => toRemoveIds.Contains(item.Id))
            .ToArrayAsync(cancellationToken);
        if (toRemove.Length == 0)
        {
            return;
        }

        dbContext.IdempotencyRecords.RemoveRange(toRemove);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private ResponsePayload BoundResponsePayload(string? responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return new ResponsePayload(null, WasTruncated: false);
        }

        var bytes = Encoding.UTF8.GetByteCount(responseJson);
        if (bytes <= Math.Max(64, _options.MaxCachedResponseBytes))
        {
            return new ResponsePayload(responseJson, WasTruncated: false);
        }

        return new ResponsePayload(null, WasTruncated: true);
    }

    private static string NormalizeKey(string value) => value.Trim();

    private const string EntryStatusPending = "Pending";
    private const string EntryStatusCompleted = "Completed";

    private sealed record ResponsePayload(string? Content, bool WasTruncated);
}
