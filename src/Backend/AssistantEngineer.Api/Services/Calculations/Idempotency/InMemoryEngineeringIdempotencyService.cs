using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations.Idempotency;

public sealed class InMemoryEngineeringIdempotencyService : IEngineeringIdempotencyService
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly EngineeringIdempotencyOptions _options;
    private readonly ILogger<InMemoryEngineeringIdempotencyService> _logger;

    public InMemoryEngineeringIdempotencyService(
        IOptions<EngineeringIdempotencyOptions> options,
        ILogger<InMemoryEngineeringIdempotencyService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<EngineeringIdempotencyEvaluationResult> EvaluateAsync(
        string? idempotencyKey,
        string scope,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Enabled || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Task.FromResult(new EngineeringIdempotencyEvaluationResult(EngineeringIdempotencyEvaluationKind.Bypass));
        }

        var normalizedKey = NormalizeKey(idempotencyKey);
        var entryKey = BuildEntryKey(scope, normalizedKey);
        var now = DateTimeOffset.UtcNow;
        CleanupExpired(now);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_entries.TryGetValue(entryKey, out var current))
            {
                var created = new Entry(
                    Scope: scope,
                    IdempotencyKey: normalizedKey,
                    RequestFingerprint: requestFingerprint,
                    CreatedAtUtc: now,
                    ExpiresAtUtc: now.AddMinutes(Math.Max(1, _options.TtlMinutes)),
                    Status: EntryStatus.Pending,
                    ResponseJson: null,
                    ResponseReferenceId: null);

                if (_entries.TryAdd(entryKey, created))
                {
                    EnforceEntryLimit();
                    _logger.LogInformation(
                        "Engineering idempotency key accepted. Scope={Scope}, Key={IdempotencyKey}",
                        scope,
                        normalizedKey);
                    return Task.FromResult(new EngineeringIdempotencyEvaluationResult(EngineeringIdempotencyEvaluationKind.Proceed));
                }

                continue;
            }

            if (!string.Equals(current.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Engineering idempotency conflict detected. Scope={Scope}, Key={IdempotencyKey}",
                    scope,
                    normalizedKey);
                return Task.FromResult(new EngineeringIdempotencyEvaluationResult(
                    EngineeringIdempotencyEvaluationKind.Conflict,
                    ConflictCode: "ENGINEERING_IDEMPOTENCY_CONFLICT",
                    ConflictMessage: "Idempotency key already exists for this scope with a different request payload."));
            }

            if (current.Status == EntryStatus.Completed)
            {
                _logger.LogInformation(
                    "Engineering idempotency replay hit. Scope={Scope}, Key={IdempotencyKey}",
                    scope,
                    normalizedKey);
                return Task.FromResult(new EngineeringIdempotencyEvaluationResult(
                    EngineeringIdempotencyEvaluationKind.Replay,
                    ReplayPayload: new EngineeringIdempotencyReplayPayload(
                        current.ResponseJson,
                        current.ResponseReferenceId)));
            }

            _logger.LogWarning(
                "Engineering idempotency key is already in progress. Scope={Scope}, Key={IdempotencyKey}",
                scope,
                normalizedKey);
            return Task.FromResult(new EngineeringIdempotencyEvaluationResult(
                EngineeringIdempotencyEvaluationKind.Conflict,
                ConflictCode: "ENGINEERING_IDEMPOTENCY_IN_PROGRESS",
                ConflictMessage: "Idempotency key is already being processed for this scope."));
        }
    }

    public Task RecordSuccessAsync(
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
            return Task.CompletedTask;
        }

        var normalizedKey = NormalizeKey(idempotencyKey);
        var entryKey = BuildEntryKey(scope, normalizedKey);
        var now = DateTimeOffset.UtcNow;

        var boundedResponse = BoundResponsePayload(responseJson);
        if (boundedResponse.WasTruncated)
        {
            _logger.LogWarning(
                "Engineering idempotency response payload exceeded cache cap and was not stored inline. Scope={Scope}, Key={IdempotencyKey}, MaxBytes={MaxBytes}",
                scope,
                normalizedKey,
                _options.MaxCachedResponseBytes);
        }

        var updated = new Entry(
            Scope: scope,
            IdempotencyKey: normalizedKey,
            RequestFingerprint: requestFingerprint,
            CreatedAtUtc: now,
            ExpiresAtUtc: now.AddMinutes(Math.Max(1, _options.TtlMinutes)),
            Status: EntryStatus.Completed,
            ResponseJson: boundedResponse.Content,
            ResponseReferenceId: responseReferenceId);

        _entries.AddOrUpdate(entryKey, updated, (_, _) => updated);
        EnforceEntryLimit();

        _logger.LogInformation(
            "Engineering idempotency response persisted. Scope={Scope}, Key={IdempotencyKey}, HasInlineResponse={HasInlineResponse}, HasReference={HasReference}",
            scope,
            normalizedKey,
            !string.IsNullOrWhiteSpace(boundedResponse.Content),
            !string.IsNullOrWhiteSpace(responseReferenceId));

        return Task.CompletedTask;
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

    private void CleanupExpired(DateTimeOffset now)
    {
        foreach (var item in _entries)
        {
            if (item.Value.ExpiresAtUtc <= now)
            {
                _entries.TryRemove(item.Key, out _);
            }
        }
    }

    private void EnforceEntryLimit()
    {
        var maxEntries = Math.Max(1, _options.MaxEntries);
        var overflow = _entries.Count - maxEntries;
        if (overflow <= 0)
        {
            return;
        }

        foreach (var candidate in _entries
                     .OrderBy(item => item.Value.CreatedAtUtc)
                     .ThenBy(item => item.Key, StringComparer.Ordinal)
                     .Take(overflow))
        {
            _entries.TryRemove(candidate.Key, out _);
        }
    }

    private static string NormalizeKey(string value) => value.Trim();

    private static string BuildEntryKey(string scope, string idempotencyKey) => $"{scope}|{idempotencyKey}";

    private enum EntryStatus
    {
        Pending,
        Completed
    }

    private sealed record Entry(
        string Scope,
        string IdempotencyKey,
        string RequestFingerprint,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        EntryStatus Status,
        string? ResponseJson,
        string? ResponseReferenceId);

    private sealed record ResponsePayload(string? Content, bool WasTruncated);
}
