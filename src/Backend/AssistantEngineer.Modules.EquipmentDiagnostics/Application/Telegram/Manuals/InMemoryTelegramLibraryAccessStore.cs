using System.Collections.Concurrent;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;

public sealed class InMemoryTelegramLibraryAccessStore : ITelegramLibraryAccessStore
{
    private readonly ConcurrentDictionary<long, TelegramLibraryAccessGrant> _grants = new();
    private readonly ConcurrentDictionary<long, TelegramLibraryAccessRequest> _requests = new();
    private long _lastRequestId;

    public Task<bool> HasActiveGrantAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _grants.TryGetValue(telegramUserDatabaseId, out var grant) &&
            grant.IsActive &&
            grant.RevokedAt is null);
    }

    public Task<TelegramLibraryAccessGrant?> GetActiveGrantAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _grants.TryGetValue(telegramUserDatabaseId, out var grant) &&
            grant.IsActive &&
            grant.RevokedAt is null
                ? grant
                : null);
    }

    public Task GrantAsync(
        long telegramUserDatabaseId,
        long grantedByTelegramUserDatabaseId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        _grants[telegramUserDatabaseId] = new TelegramLibraryAccessGrant(
            telegramUserDatabaseId,
            grantedByTelegramUserDatabaseId,
            true,
            now,
            now,
            null,
            reason);
        return Task.CompletedTask;
    }

    public Task<bool> RevokeAsync(
        long telegramUserDatabaseId,
        long revokedByTelegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_grants.TryGetValue(telegramUserDatabaseId, out var grant) ||
            !grant.IsActive ||
            grant.RevokedAt is not null)
        {
            return Task.FromResult(false);
        }

        var now = DateTimeOffset.UtcNow;
        _grants[telegramUserDatabaseId] = grant with
        {
            IsActive = false,
            UpdatedAt = now,
            RevokedAt = now
        };
        return Task.FromResult(true);
    }

    public Task<TelegramLibraryAccessRequest> CreateOrGetPendingRequestAsync(
        TelegramUserSnapshot user,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = _requests.Values
            .Where(request =>
                request.TelegramUserId == user.Id &&
                request.Status == TelegramLibraryAccessRequestStatus.Pending)
            .OrderByDescending(request => request.CreatedAt)
            .FirstOrDefault();
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var request = new TelegramLibraryAccessRequest(
            Interlocked.Increment(ref _lastRequestId),
            user.Id,
            user.TelegramChatId,
            user.Role,
            TelegramLibraryAccessRequestStatus.Pending,
            message,
            now,
            null,
            null);
        _requests[request.Id] = request;
        return Task.FromResult(request);
    }

    public Task<IReadOnlyList<TelegramLibraryAccessRequest>> ListPendingRequestsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<TelegramLibraryAccessRequest>>(
            _requests.Values
                .Where(request => request.Status == TelegramLibraryAccessRequestStatus.Pending)
                .OrderBy(request => request.CreatedAt)
                .Take(Math.Clamp(limit, 1, 100))
                .ToArray());
    }

    public Task<TelegramLibraryAccessRequest?> GetRequestAsync(
        long requestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_requests.TryGetValue(requestId, out var request) ? request : null);
    }

    public Task<TelegramLibraryAccessRequest?> ResolveRequestAsync(
        long requestId,
        TelegramLibraryAccessRequestStatus status,
        long resolvedByTelegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_requests.TryGetValue(requestId, out var request) ||
            request.Status != TelegramLibraryAccessRequestStatus.Pending)
        {
            return Task.FromResult<TelegramLibraryAccessRequest?>(request);
        }

        var resolved = request with
        {
            Status = status,
            ResolvedAt = DateTimeOffset.UtcNow,
            ResolvedByTelegramUserId = resolvedByTelegramUserDatabaseId
        };
        _requests[requestId] = resolved;
        return Task.FromResult<TelegramLibraryAccessRequest?>(resolved);
    }
}
