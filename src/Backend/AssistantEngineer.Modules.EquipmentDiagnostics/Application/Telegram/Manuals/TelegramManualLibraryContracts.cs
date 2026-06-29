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
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
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
    DateTimeOffset? UpdatedAtUtc = null);

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
