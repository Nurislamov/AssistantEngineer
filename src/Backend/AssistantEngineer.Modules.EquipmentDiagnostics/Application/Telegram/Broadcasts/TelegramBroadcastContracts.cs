using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;

public enum TelegramBroadcastAudienceKind
{
    AllActive,
    Role
}

public enum TelegramBroadcastCampaignStatus
{
    Draft,
    Ready,
    Sending,
    Completed,
    Cancelled,
    Failed
}

public enum TelegramBroadcastRecipientStatus
{
    Pending,
    Sent,
    Skipped,
    Failed
}

public enum TelegramBroadcastAttachmentType
{
    Photo,
    Document,
    Video
}

public sealed class TelegramBroadcastCampaignEntity
{
    public long Id { get; set; }
    public long CreatedByTelegramUserId { get; set; }
    public long? CreatedByTelegramChatId { get; set; }
    public TelegramBroadcastAudienceKind AudienceKind { get; set; }
    public TelegramUserRole? AudienceRole { get; set; }
    public string Text { get; set; } = string.Empty;
    public TelegramBroadcastCampaignStatus Status { get; set; } = TelegramBroadcastCampaignStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public string? LastError { get; set; }
}

public sealed class TelegramBroadcastRecipientEntity
{
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public long TelegramUserId { get; set; }
    public long? TelegramChatId { get; set; }
    public TelegramUserRole Role { get; set; }
    public TelegramBroadcastRecipientStatus Status { get; set; } = TelegramBroadcastRecipientStatus.Pending;
    public string? SkipReason { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TelegramBroadcastAttachmentEntity
{
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public TelegramBroadcastAttachmentType AttachmentType { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string? FileUniqueId { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record TelegramBroadcastCampaignSnapshot(
    long Id,
    long CreatedByTelegramUserId,
    long? CreatedByTelegramChatId,
    TelegramBroadcastAudienceKind AudienceKind,
    TelegramUserRole? AudienceRole,
    string Text,
    TelegramBroadcastCampaignStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int TotalRecipients,
    int SentCount,
    int SkippedCount,
    int FailedCount,
    string? LastError);

public sealed record TelegramBroadcastRecipientCreate(
    long TelegramUserId,
    long? TelegramChatId,
    TelegramUserRole Role,
    TelegramBroadcastRecipientStatus Status,
    string? SkipReason = null);

public sealed record TelegramBroadcastRecipientUpdate(
    long RecipientId,
    TelegramBroadcastRecipientStatus Status,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    DateTimeOffset? SentAt = null);

public sealed record TelegramBroadcastRecipientSnapshot(
    long Id,
    long CampaignId,
    long TelegramUserId,
    long? TelegramChatId,
    TelegramUserRole Role,
    TelegramBroadcastRecipientStatus Status,
    string? SkipReason,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt);

public sealed record TelegramBroadcastAttachmentCreate(
    TelegramBroadcastAttachmentType AttachmentType,
    string FileId,
    string? FileUniqueId,
    string? FileName,
    string? MimeType,
    long? FileSize,
    int SortOrder);

public sealed record TelegramBroadcastAttachmentSnapshot(
    long Id,
    long CampaignId,
    TelegramBroadcastAttachmentType AttachmentType,
    string FileId,
    string? FileUniqueId,
    string? FileName,
    string? MimeType,
    long? FileSize,
    int SortOrder,
    DateTimeOffset CreatedAt);

public interface ITelegramBroadcastStore
{
    Task<TelegramBroadcastCampaignSnapshot> CreateDraftAsync(
        long createdByTelegramUserId,
        long? createdByTelegramChatId,
        TelegramBroadcastAudienceKind audienceKind,
        TelegramUserRole? audienceRole,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<TelegramBroadcastCampaignSnapshot?> GetCampaignAsync(
        long campaignId,
        CancellationToken cancellationToken = default);

    Task<TelegramBroadcastCampaignSnapshot?> SetReadyAsync(
        long campaignId,
        string text,
        int totalRecipients,
        int skippedCount,
        CancellationToken cancellationToken = default);

    Task<TelegramBroadcastCampaignSnapshot?> MarkSendingAsync(
        long campaignId,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken = default);

    Task<TelegramBroadcastCampaignSnapshot?> CompleteAsync(
        long campaignId,
        int sentCount,
        int skippedCount,
        int failedCount,
        string? lastError,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    Task<TelegramBroadcastCampaignSnapshot?> CancelAsync(
        long campaignId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramBroadcastRecipientSnapshot>> ReplaceRecipientsAsync(
        long campaignId,
        IReadOnlyList<TelegramBroadcastRecipientCreate> recipients,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<TelegramBroadcastRecipientSnapshot?> UpdateRecipientAsync(
        TelegramBroadcastRecipientUpdate update,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramBroadcastRecipientSnapshot>> ListRecipientsAsync(
        long campaignId,
        CancellationToken cancellationToken = default);

    Task<TelegramBroadcastAttachmentSnapshot> AddAttachmentAsync(
        long campaignId,
        TelegramBroadcastAttachmentCreate attachment,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramBroadcastAttachmentSnapshot>> ListAttachmentsAsync(
        long campaignId,
        CancellationToken cancellationToken = default);
}
