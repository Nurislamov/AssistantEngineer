using System.Collections.Concurrent;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public sealed class InMemoryTelegramServiceRequestStore : ITelegramServiceRequestStore
{
    private readonly ConcurrentDictionary<long, TelegramServiceRequestEntity> _requests = new();
    private readonly object _gate = new();
    private long _nextId;

    public Task<TelegramServiceRequestCreateResult> CreateIfNoActiveAsync(
        TelegramServiceRequestCreate request,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var existing = _requests.Values
                .Where(item => item.DiagnosticCaseId == request.DiagnosticCaseId)
                .Where(item => item.Status is TelegramServiceRequestStatus.New or TelegramServiceRequestStatus.InProgress)
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefault();
            if (existing is not null)
            {
                return Task.FromResult(new TelegramServiceRequestCreateResult(ToSnapshot(existing), false));
            }

            var entity = new TelegramServiceRequestEntity
            {
                Id = Interlocked.Increment(ref _nextId),
                TelegramUserId = request.TelegramUserId,
                DiagnosticCaseId = request.DiagnosticCaseId,
                Status = TelegramServiceRequestStatus.New,
                Code = request.Code,
                Manufacturer = request.Manufacturer,
                EquipmentType = request.EquipmentType,
                DisplayContext = request.DisplayContext,
                PhoneWasSaved = request.PhoneWasSaved,
                PhoneNumberSource = request.PhoneNumberSource,
                UserRoleAtCreation = request.UserRoleAtCreation,
                CreatedAt = request.CreatedAt
            };
            _requests[entity.Id] = entity;
            return Task.FromResult(new TelegramServiceRequestCreateResult(ToSnapshot(entity), true));
        }
    }

    public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestForTelegramUserAsync(
        long telegramUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var requests = _requests.Values
            .Where(item => item.TelegramUserId == telegramUserId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 20))
            .Select(ToSnapshot)
            .ToArray();
        return Task.FromResult<IReadOnlyList<TelegramServiceRequestSnapshot>>(requests);
    }

    public Task<TelegramServiceRequestSnapshot?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_requests.TryGetValue(id, out var request) ? ToSnapshot(request) : null);

    public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetActiveAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var requests = _requests.Values
            .Where(item => item.Status is TelegramServiceRequestStatus.New or TelegramServiceRequestStatus.InProgress)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(ToSnapshot)
            .ToArray();
        return Task.FromResult<IReadOnlyList<TelegramServiceRequestSnapshot>>(requests);
    }

    public Task<TelegramServiceRequestSnapshot?> UpdateAsync(
        TelegramServiceRequestUpdate update,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_requests.TryGetValue(update.Id, out var entity))
            {
                return Task.FromResult<TelegramServiceRequestSnapshot?>(null);
            }

            entity.Status = update.Status;
            entity.AssignedTelegramUserId = update.AssignedTelegramUserId;
            entity.AssignedAt = update.AssignedAt;
            entity.AssignedByTelegramUserId = update.AssignedByTelegramUserId;
            entity.StatusUpdatedAt = update.StatusUpdatedAt;
            entity.StatusUpdatedByTelegramUserId = update.StatusUpdatedByTelegramUserId;
            entity.UpdatedAt = update.StatusUpdatedAt;
            entity.ClosedAt = update.ClosedAt;
            return Task.FromResult<TelegramServiceRequestSnapshot?>(ToSnapshot(entity));
        }
    }

    public Task<TelegramServiceRequestSnapshot?> UpdateNotificationAsync(
        TelegramServiceRequestNotificationUpdate update,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_requests.TryGetValue(update.Id, out var entity))
            {
                return Task.FromResult<TelegramServiceRequestSnapshot?>(null);
            }

            entity.NotificationChatId = update.NotificationChatId;
            entity.NotificationMessageId = update.NotificationMessageId;
            entity.NotificationSentAt = update.NotificationSentAt;
            entity.NotificationUpdatedAt = update.NotificationUpdatedAt;
            return Task.FromResult<TelegramServiceRequestSnapshot?>(ToSnapshot(entity));
        }
    }

    private static TelegramServiceRequestSnapshot ToSnapshot(TelegramServiceRequestEntity entity) =>
        new(
            entity.Id,
            entity.TelegramUserId,
            entity.DiagnosticCaseId,
            entity.Source,
            entity.Status,
            entity.Code,
            entity.Manufacturer,
            entity.EquipmentType,
            entity.DisplayContext,
            entity.PhoneWasSaved,
            entity.PhoneNumberSource,
            entity.ContactPhoneLast4,
            entity.UserRoleAtCreation,
            entity.AssignedTelegramUserId,
            entity.AssignedAt,
            entity.AssignedByTelegramUserId,
            entity.StatusUpdatedAt,
            entity.StatusUpdatedByTelegramUserId,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.ClosedAt,
            entity.NotificationChatId,
            entity.NotificationMessageId,
            entity.NotificationSentAt,
            entity.NotificationUpdatedAt);
}
