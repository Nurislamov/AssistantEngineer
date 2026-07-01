using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public enum TelegramServiceRequestMessageDirection
{
    UserToOperator,
    OperatorToUser,
    System
}

public enum TelegramServiceRequestPendingKind
{
    OperatorReply,
    UserRequestSelection
}

public enum TelegramServiceRequestAttachmentType
{
    Photo,
    Document,
    Video
}

public sealed class TelegramServiceRequestMessageEntity
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public TelegramServiceRequestMessageDirection Direction { get; set; }
    public long? SenderTelegramUserId { get; set; }
    public TelegramUserRole? SenderRole { get; set; }
    public string Text { get; set; } = string.Empty;
    public long? TelegramChatId { get; set; }
    public long? TelegramMessageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TelegramServiceRequestPendingEntity
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public TelegramServiceRequestPendingKind Kind { get; set; }
    public long? ServiceRequestId { get; set; }
    public string? PendingText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class TelegramServiceRequestMessageAttachmentEntity
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public TelegramServiceRequestAttachmentType AttachmentType { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string? FileUniqueId { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record TelegramServiceRequestMessageCreate(
    long ServiceRequestId,
    TelegramServiceRequestMessageDirection Direction,
    long? SenderTelegramUserId,
    TelegramUserRole? SenderRole,
    string Text,
    long? TelegramChatId,
    long? TelegramMessageId,
    DateTimeOffset CreatedAt);

public sealed record TelegramServiceRequestMessageSnapshot(
    long Id,
    long ServiceRequestId,
    TelegramServiceRequestMessageDirection Direction,
    long? SenderTelegramUserId,
    TelegramUserRole? SenderRole,
    string Text,
    long? TelegramChatId,
    long? TelegramMessageId,
    DateTimeOffset CreatedAt);

public sealed record TelegramServiceRequestPendingSnapshot(
    long Id,
    long TelegramUserId,
    TelegramServiceRequestPendingKind Kind,
    long? ServiceRequestId,
    string? PendingText,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public sealed record TelegramServiceRequestMessageAttachmentCreate(
    long MessageId,
    TelegramServiceRequestAttachmentType AttachmentType,
    string FileId,
    string? FileUniqueId,
    string? FileName,
    string? MimeType,
    long? FileSize,
    int? Width,
    int? Height,
    int? Duration,
    DateTimeOffset CreatedAt);

public sealed record TelegramServiceRequestMessageAttachmentSnapshot(
    long Id,
    long MessageId,
    TelegramServiceRequestAttachmentType AttachmentType,
    string FileId,
    string? FileUniqueId,
    string? FileName,
    string? MimeType,
    long? FileSize,
    int? Width,
    int? Height,
    int? Duration,
    DateTimeOffset CreatedAt);

public interface ITelegramServiceRequestDialogStore
{
    Task<TelegramServiceRequestMessageSnapshot> AddMessageAsync(
        TelegramServiceRequestMessageCreate message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramServiceRequestMessageSnapshot>> GetLatestMessagesAsync(
        long serviceRequestId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> HasOperatorReplyAsync(
        long serviceRequestId,
        CancellationToken cancellationToken = default);

    Task<TelegramServiceRequestMessageAttachmentSnapshot> AddAttachmentAsync(
        TelegramServiceRequestMessageAttachmentCreate attachment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramServiceRequestMessageAttachmentSnapshot>> GetAttachmentsAsync(
        long messageId,
        CancellationToken cancellationToken = default);

    Task<TelegramServiceRequestPendingSnapshot?> GetPendingAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<TelegramServiceRequestPendingSnapshot> SetPendingAsync(
        long telegramUserId,
        TelegramServiceRequestPendingKind kind,
        long? serviceRequestId,
        string? pendingText,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task ClearPendingAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);
}
