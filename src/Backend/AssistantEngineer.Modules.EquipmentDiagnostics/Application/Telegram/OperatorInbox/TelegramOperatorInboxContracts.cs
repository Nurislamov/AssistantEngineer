using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;

public enum TelegramOperatorInboxThreadStatus
{
    Open,
    Answered
}

public enum TelegramOperatorInboxMessageDirection
{
    UserToOperator,
    OperatorToUser,
    System
}

public enum TelegramOperatorInboxMessageKind
{
    Text,
    Photo,
    Video,
    Document,
    Voice,
    Unknown
}

public sealed class TelegramOperatorInboxThreadEntity
{
    public long Id { get; set; }
    public long? TelegramUserId { get; set; }
    public long TelegramChatId { get; set; }
    public string? UserDisplayName { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public TelegramOperatorInboxThreadStatus Status { get; set; } = TelegramOperatorInboxThreadStatus.Open;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastUserMessageAt { get; set; }
    public DateTimeOffset? LastOwnerReplyAt { get; set; }
}

public sealed class TelegramOperatorInboxMessageEntity
{
    public long Id { get; set; }
    public long ThreadId { get; set; }
    public TelegramOperatorInboxMessageDirection Direction { get; set; }
    public long? UserChatId { get; set; }
    public long? UserMessageId { get; set; }
    public long? OperatorChatId { get; set; }
    public long? OperatorMessageId { get; set; }
    public long? OperatorReplyToMessageId { get; set; }
    public TelegramOperatorInboxMessageKind MessageKind { get; set; } = TelegramOperatorInboxMessageKind.Unknown;
    public string? Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record TelegramOperatorInboxThreadSnapshot(
    long Id,
    long? TelegramUserId,
    long TelegramChatId,
    string? UserDisplayName,
    string? Username,
    string? UserRole,
    TelegramOperatorInboxThreadStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastUserMessageAt,
    DateTimeOffset? LastOwnerReplyAt);

public sealed record TelegramOperatorInboxMessageSnapshot(
    long Id,
    long ThreadId,
    TelegramOperatorInboxMessageDirection Direction,
    long? UserChatId,
    long? UserMessageId,
    long? OperatorChatId,
    long? OperatorMessageId,
    long? OperatorReplyToMessageId,
    TelegramOperatorInboxMessageKind MessageKind,
    string? Text,
    DateTimeOffset CreatedAt);

public sealed record TelegramOperatorInboxUserMessage(
    TelegramOperatorInboxThreadSnapshot Thread,
    TelegramOperatorInboxMessageSnapshot Message);

public interface ITelegramOperatorInboxStore
{
    Task<TelegramOperatorInboxUserMessage> AddUserMessageAsync(
        TelegramUserAccessResult access,
        EquipmentDiagnosticTelegramUpdate update,
        TelegramOperatorInboxMessageKind kind,
        string? text,
        CancellationToken cancellationToken = default);

    Task SetOperatorMessageAsync(
        long messageId,
        long operatorChatId,
        long operatorMessageId,
        CancellationToken cancellationToken = default);

    Task<TelegramOperatorInboxMessageSnapshot> AddOperatorMirrorAsync(
        long threadId,
        long? userChatId,
        long? userMessageId,
        long operatorChatId,
        long operatorMessageId,
        TelegramOperatorInboxMessageKind kind,
        string? text,
        CancellationToken cancellationToken = default);

    Task<TelegramOperatorInboxMessageSnapshot?> GetByOperatorMessageAsync(
        long operatorChatId,
        long operatorMessageId,
        CancellationToken cancellationToken = default);

    Task<TelegramOperatorInboxThreadSnapshot?> GetThreadAsync(
        long threadId,
        CancellationToken cancellationToken = default);

    Task<TelegramOperatorInboxMessageSnapshot> AddOperatorReplyAsync(
        long threadId,
        long userChatId,
        long operatorChatId,
        long operatorMessageId,
        long operatorReplyToMessageId,
        string text,
        CancellationToken cancellationToken = default);
}

public interface ITelegramOperatorInboxService
{
    Task<bool> TryHandleOperatorCommandAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default);

    Task<bool> TryHandleOperatorReplyAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default);

    Task MirrorUserMessageAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default);
}
