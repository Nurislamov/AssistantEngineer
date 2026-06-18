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
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.ClosedAt);
}
