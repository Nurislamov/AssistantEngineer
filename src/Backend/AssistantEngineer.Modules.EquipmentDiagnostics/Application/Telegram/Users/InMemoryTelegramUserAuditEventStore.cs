using System.Collections.Concurrent;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public sealed class InMemoryTelegramUserAuditEventStore : ITelegramUserAuditEventStore
{
    private readonly ConcurrentDictionary<long, TelegramUserAuditEventEntity> _events = new();
    private long _nextId;

    public Task<TelegramUserAuditEventSnapshot> AppendAsync(
        TelegramUserAuditEventCreate request,
        CancellationToken cancellationToken = default)
    {
        var entity = new TelegramUserAuditEventEntity
        {
            Id = Interlocked.Increment(ref _nextId),
            EventType = request.EventType,
            ActorTelegramUserId = request.ActorTelegramUserId,
            TargetTelegramUserId = request.TargetTelegramUserId,
            OldRole = request.OldRole,
            NewRole = request.NewRole,
            OldIsEnabled = request.OldIsEnabled,
            NewIsEnabled = request.NewIsEnabled,
            OldIsBlocked = request.OldIsBlocked,
            NewIsBlocked = request.NewIsBlocked,
            IsSuccessful = request.IsSuccessful,
            Message = request.Message,
            MetadataJson = request.MetadataJson,
            CreatedAt = request.CreatedAt
        };
        _events[entity.Id] = entity;
        return Task.FromResult(ToSnapshot(entity));
    }

    public Task<IReadOnlyList<TelegramUserAuditEventSnapshot>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var events = _events.Values
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(ToSnapshot)
            .ToArray();
        return Task.FromResult<IReadOnlyList<TelegramUserAuditEventSnapshot>>(events);
    }

    private static TelegramUserAuditEventSnapshot ToSnapshot(TelegramUserAuditEventEntity entity) =>
        new(
            entity.Id,
            entity.EventType,
            entity.ActorTelegramUserId,
            entity.TargetTelegramUserId,
            entity.OldRole,
            entity.NewRole,
            entity.OldIsEnabled,
            entity.NewIsEnabled,
            entity.OldIsBlocked,
            entity.NewIsBlocked,
            entity.IsSuccessful,
            entity.Message,
            entity.MetadataJson,
            entity.CreatedAt);
}
