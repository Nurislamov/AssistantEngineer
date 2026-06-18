using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public enum TelegramServiceRequestSource
{
    Telegram
}

public enum TelegramServiceRequestStatus
{
    New,
    InProgress,
    Resolved,
    Cancelled
}

public sealed class TelegramServiceRequestEntity
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public long DiagnosticCaseId { get; set; }
    public TelegramServiceRequestSource Source { get; set; } = TelegramServiceRequestSource.Telegram;
    public TelegramServiceRequestStatus Status { get; set; } = TelegramServiceRequestStatus.New;
    public string Code { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? EquipmentType { get; set; }
    public string? DisplayContext { get; set; }
    public bool PhoneWasSaved { get; set; }
    public TelegramUserPhoneNumberSource? PhoneNumberSource { get; set; }
    public string? ContactPhoneLast4 { get; set; }
    public TelegramUserRole UserRoleAtCreation { get; set; }
    public long? AssignedTelegramUserId { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public long? AssignedByTelegramUserId { get; set; }
    public DateTimeOffset? StatusUpdatedAt { get; set; }
    public long? StatusUpdatedByTelegramUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public long? NotificationChatId { get; set; }
    public long? NotificationMessageId { get; set; }
    public DateTimeOffset? NotificationSentAt { get; set; }
    public DateTimeOffset? NotificationUpdatedAt { get; set; }
}

public sealed record TelegramServiceRequestSnapshot(
    long Id,
    long TelegramUserId,
    long DiagnosticCaseId,
    TelegramServiceRequestSource Source,
    TelegramServiceRequestStatus Status,
    string Code,
    string? Manufacturer,
    string? EquipmentType,
    string? DisplayContext,
    bool PhoneWasSaved,
    TelegramUserPhoneNumberSource? PhoneNumberSource,
    string? ContactPhoneLast4,
    TelegramUserRole UserRoleAtCreation,
    long? AssignedTelegramUserId,
    DateTimeOffset? AssignedAt,
    long? AssignedByTelegramUserId,
    DateTimeOffset? StatusUpdatedAt,
    long? StatusUpdatedByTelegramUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ClosedAt,
    long? NotificationChatId,
    long? NotificationMessageId,
    DateTimeOffset? NotificationSentAt,
    DateTimeOffset? NotificationUpdatedAt);

public sealed record TelegramServiceRequestCreate(
    long TelegramUserId,
    long DiagnosticCaseId,
    string Code,
    string? Manufacturer,
    string? EquipmentType,
    string? DisplayContext,
    bool PhoneWasSaved,
    TelegramUserPhoneNumberSource? PhoneNumberSource,
    TelegramUserRole UserRoleAtCreation,
    DateTimeOffset CreatedAt);

public sealed record TelegramServiceRequestCreateResult(
    TelegramServiceRequestSnapshot Request,
    bool Created);

public sealed record TelegramServiceRequestUpdate(
    long Id,
    TelegramServiceRequestStatus Status,
    long? AssignedTelegramUserId,
    DateTimeOffset? AssignedAt,
    long? AssignedByTelegramUserId,
    DateTimeOffset StatusUpdatedAt,
    long StatusUpdatedByTelegramUserId,
    DateTimeOffset? ClosedAt);

public sealed record TelegramServiceRequestNotificationUpdate(
    long Id,
    long NotificationChatId,
    long NotificationMessageId,
    DateTimeOffset NotificationSentAt,
    DateTimeOffset NotificationUpdatedAt);

public interface ITelegramServiceRequestStore
{
    Task<TelegramServiceRequestCreateResult> CreateIfNoActiveAsync(
        TelegramServiceRequestCreate request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestForTelegramUserAsync(
        long telegramUserId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<TelegramServiceRequestSnapshot?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetActiveAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<TelegramServiceRequestSnapshot?> UpdateAsync(
        TelegramServiceRequestUpdate update,
        CancellationToken cancellationToken = default);

    Task<TelegramServiceRequestSnapshot?> UpdateNotificationAsync(
        TelegramServiceRequestNotificationUpdate update,
        CancellationToken cancellationToken = default);
}

public enum TelegramServiceRequestAttemptStatus
{
    Created,
    Existing,
    NoDiagnosticCase,
    PhoneMissing
}

public sealed record TelegramServiceRequestAttemptResult(
    TelegramServiceRequestAttemptStatus Status,
    TelegramServiceRequestSnapshot? Request,
    string Text);
