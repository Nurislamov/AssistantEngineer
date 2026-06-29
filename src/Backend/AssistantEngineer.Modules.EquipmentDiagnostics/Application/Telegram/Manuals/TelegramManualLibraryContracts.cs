using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;

public sealed record TelegramManualRegistryEntry(
    string ManualId,
    string FileName,
    string DocumentTitle,
    string? DisplayNameRu,
    string? DocumentCode,
    string FileFormat,
    bool EligibleForTelegramLibrary,
    IReadOnlySet<TelegramUserRole> AllowedRoles,
    IReadOnlySet<TelegramUserRole> DeniedRoles);

public enum TelegramLibraryDocumentType
{
    OwnerManual,
    UserGuide,
    InstallationManual,
    CommissioningGuide,
    WiringDiagram,
    ServiceManual,
    ErrorCodeTable,
    InternalNote
}

public enum TelegramLibraryAccessRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public sealed class TelegramManualBindingEntity
{
    public long Id { get; set; }
    public string? ManualId { get; set; }
    public string? Brand { get; set; }
    public string? Series { get; set; }
    public string TelegramFileId { get; set; } = string.Empty;
    public string? TelegramFileUniqueId { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public long? UploadedByTelegramUserId { get; set; }
    public long? UploadedByTelegramChatId { get; set; }
    public string? RegisteredByRole { get; set; }
    public string Source { get; set; } = "TelegramManualBind";
    public string? Title { get; set; }
    public TelegramLibraryDocumentType DocumentType { get; set; } = TelegramLibraryDocumentType.ServiceManual;
    public TelegramUserRole MinRole { get; set; } = TelegramUserRole.Engineer;
    public bool IsLibraryVisible { get; set; } = true;
    public bool CanUseForDiagnostics { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TelegramLibraryAccessGrantEntity
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public long? GrantedByTelegramUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class TelegramLibraryAccessRequestEntity
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public long TelegramChatId { get; set; }
    public TelegramUserRole RequestedRole { get; set; }
    public TelegramLibraryAccessRequestStatus Status { get; set; } = TelegramLibraryAccessRequestStatus.Pending;
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public long? ResolvedByTelegramUserId { get; set; }
}

public sealed record TelegramManualFileBinding(
    string ManualId,
    string TelegramFileId,
    string? OriginalFileName,
    string? ContentType,
    DateTimeOffset RegisteredAtUtc,
    string Source,
    string? RegisteredByRole,
    string? TelegramFileUniqueId = null,
    long? FileSize = null,
    string? Brand = null,
    string? Series = null,
    long? UploadedByTelegramUserId = null,
    long? UploadedByTelegramChatId = null,
    bool IsActive = true,
    DateTimeOffset? UpdatedAtUtc = null,
    string? Title = null,
    TelegramLibraryDocumentType DocumentType = TelegramLibraryDocumentType.ServiceManual,
    TelegramUserRole MinRole = TelegramUserRole.Engineer,
    bool IsLibraryVisible = true,
    bool CanUseForDiagnostics = false);

public sealed record TelegramLibraryAccessGrant(
    long TelegramUserId,
    long? GrantedByTelegramUserId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RevokedAt,
    string? Reason);

public sealed record TelegramLibraryAccessRequest(
    long Id,
    long TelegramUserId,
    long TelegramChatId,
    TelegramUserRole RequestedRole,
    TelegramLibraryAccessRequestStatus Status,
    string? Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    long? ResolvedByTelegramUserId);

public sealed record TelegramManualLibraryResult(
    string Text,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? Messages = null,
    string? ParseMode = null,
    string? CallbackAnswerText = null,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null);

public interface ITelegramManualRegistrySource
{
    IReadOnlyList<TelegramManualRegistryEntry> GetManuals();
}

public interface ITelegramManualFileBindingStore
{
    Task<TelegramManualFileBinding?> GetAsync(
        string manualId,
        CancellationToken cancellationToken = default);

    Task<TelegramManualFileBinding?> GetBySeriesAsync(
        string brand,
        string series,
        CancellationToken cancellationToken = default);

    Task<TelegramManualFileBinding?> GetDiagnosticBySeriesAsync(
        string brand,
        string series,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramManualFileBinding>> ListAsync(
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        TelegramManualFileBinding binding,
        CancellationToken cancellationToken = default);

    Task UpsertSeriesAsync(
        TelegramManualFileBinding binding,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(
        string manualId,
        CancellationToken cancellationToken = default);
}

public interface ITelegramLibraryAccessStore
{
    Task<bool> HasActiveGrantAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default);

    Task<TelegramLibraryAccessGrant?> GetActiveGrantAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default);

    Task GrantAsync(
        long telegramUserDatabaseId,
        long grantedByTelegramUserDatabaseId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        long telegramUserDatabaseId,
        long revokedByTelegramUserDatabaseId,
        CancellationToken cancellationToken = default);

    Task<TelegramLibraryAccessRequest> CreateOrGetPendingRequestAsync(
        TelegramUserSnapshot user,
        string? message = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramLibraryAccessRequest>> ListPendingRequestsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<TelegramLibraryAccessRequest?> GetRequestAsync(
        long requestId,
        CancellationToken cancellationToken = default);

    Task<TelegramLibraryAccessRequest?> ResolveRequestAsync(
        long requestId,
        TelegramLibraryAccessRequestStatus status,
        long resolvedByTelegramUserDatabaseId,
        CancellationToken cancellationToken = default);
}
