using System.Collections.Concurrent;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public sealed class InMemoryTelegramServiceRequestEventStore : ITelegramServiceRequestEventStore
{
    private readonly ConcurrentDictionary<long, TelegramServiceRequestEventEntity> _events = new();
    private long _nextId;

    public Task<TelegramServiceRequestEventSnapshot> AppendAsync(
        TelegramServiceRequestEventCreate request,
        CancellationToken cancellationToken = default)
    {
        var entity = new TelegramServiceRequestEventEntity
        {
            Id = Interlocked.Increment(ref _nextId),
            ServiceRequestId = request.ServiceRequestId,
            EventType = request.EventType,
            ActorTelegramUserId = request.ActorTelegramUserId,
            TargetTelegramUserId = request.TargetTelegramUserId,
            OldStatus = request.OldStatus,
            NewStatus = request.NewStatus,
            IsSuccessful = request.IsSuccessful,
            Message = request.Message,
            MetadataJson = request.MetadataJson,
            CreatedAt = request.CreatedAt
        };
        _events[entity.Id] = entity;
        return Task.FromResult(ToSnapshot(entity));
    }

    public Task<IReadOnlyList<TelegramServiceRequestEventSnapshot>> GetLatestAsync(
        long serviceRequestId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var events = _events.Values
            .Where(item => item.ServiceRequestId == serviceRequestId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .TakeLast(Math.Clamp(limit, 1, 100))
            .Select(ToSnapshot)
            .ToArray();
        return Task.FromResult<IReadOnlyList<TelegramServiceRequestEventSnapshot>>(events);
    }

    private static TelegramServiceRequestEventSnapshot ToSnapshot(TelegramServiceRequestEventEntity entity) =>
        new(
            entity.Id,
            entity.ServiceRequestId,
            entity.EventType,
            entity.ActorTelegramUserId,
            entity.TargetTelegramUserId,
            entity.OldStatus,
            entity.NewStatus,
            entity.IsSuccessful,
            entity.Message,
            entity.MetadataJson,
            entity.CreatedAt);
}
