namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public enum TelegramUserAuditEventType
{
    RoleChanged,
    UserEnabled,
    UserDisabled,
    UserBlocked,
    UserUnblocked,
    UserActionDenied
}

public sealed class TelegramUserAuditEventEntity
{
    public long Id { get; set; }
    public TelegramUserAuditEventType EventType { get; set; }
    public long? ActorTelegramUserId { get; set; }
    public long? TargetTelegramUserId { get; set; }
    public TelegramUserRole? OldRole { get; set; }
    public TelegramUserRole? NewRole { get; set; }
    public bool? OldIsEnabled { get; set; }
    public bool? NewIsEnabled { get; set; }
    public bool? OldIsBlocked { get; set; }
    public bool? NewIsBlocked { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? Message { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record TelegramUserAuditEventSnapshot(
    long Id,
    TelegramUserAuditEventType EventType,
    long? ActorTelegramUserId,
    long? TargetTelegramUserId,
    TelegramUserRole? OldRole,
    TelegramUserRole? NewRole,
    bool? OldIsEnabled,
    bool? NewIsEnabled,
    bool? OldIsBlocked,
    bool? NewIsBlocked,
    bool IsSuccessful,
    string? Message,
    string? MetadataJson,
    DateTimeOffset CreatedAt);

public sealed record TelegramUserAuditEventCreate(
    TelegramUserAuditEventType EventType,
    long? ActorTelegramUserId,
    long? TargetTelegramUserId,
    TelegramUserRole? OldRole,
    TelegramUserRole? NewRole,
    bool? OldIsEnabled,
    bool? NewIsEnabled,
    bool? OldIsBlocked,
    bool? NewIsBlocked,
    bool IsSuccessful,
    string? Message,
    string? MetadataJson,
    DateTimeOffset CreatedAt);

public interface ITelegramUserAuditEventStore
{
    Task<TelegramUserAuditEventSnapshot> AppendAsync(
        TelegramUserAuditEventCreate request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramUserAuditEventSnapshot>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
