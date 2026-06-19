using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public enum TelegramServiceRequestEventType
{
    Created,
    NotificationSent,
    NotificationFailed,
    Taken,
    Assigned,
    Reassigned,
    ContactRequested,
    ContactSent,
    ContactFailed,
    ContactDenied,
    HistoryViewed,
    HistoryDenied,
    ActionDenied,
    Resolved,
    Cancelled,
    CustomerNotificationSent,
    CustomerNotificationFailed
}

public sealed class TelegramServiceRequestEventEntity
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public TelegramServiceRequestEventType EventType { get; set; }
    public long? ActorTelegramUserId { get; set; }
    public long? TargetTelegramUserId { get; set; }
    public TelegramServiceRequestStatus? OldStatus { get; set; }
    public TelegramServiceRequestStatus? NewStatus { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? Message { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record TelegramServiceRequestEventSnapshot(
    long Id,
    long ServiceRequestId,
    TelegramServiceRequestEventType EventType,
    long? ActorTelegramUserId,
    long? TargetTelegramUserId,
    TelegramServiceRequestStatus? OldStatus,
    TelegramServiceRequestStatus? NewStatus,
    bool IsSuccessful,
    string? Message,
    string? MetadataJson,
    DateTimeOffset CreatedAt);

public sealed record TelegramServiceRequestEventCreate(
    long ServiceRequestId,
    TelegramServiceRequestEventType EventType,
    long? ActorTelegramUserId,
    long? TargetTelegramUserId,
    TelegramServiceRequestStatus? OldStatus,
    TelegramServiceRequestStatus? NewStatus,
    bool IsSuccessful,
    string? Message,
    string? MetadataJson,
    DateTimeOffset CreatedAt);

public interface ITelegramServiceRequestEventStore
{
    Task<TelegramServiceRequestEventSnapshot> AppendAsync(
        TelegramServiceRequestEventCreate request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramServiceRequestEventSnapshot>> GetLatestAsync(
        long serviceRequestId,
        int limit,
        CancellationToken cancellationToken = default);
}
