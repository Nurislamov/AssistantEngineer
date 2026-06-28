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

public sealed record TelegramManualFileBinding(
    string ManualId,
    string TelegramFileId,
    string? OriginalFileName,
    string? ContentType,
    DateTimeOffset RegisteredAtUtc,
    string Source,
    string? RegisteredByRole);

public sealed record TelegramManualLibraryResult(
    string Text,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? Messages = null,
    string? ParseMode = null,
    string? CallbackAnswerText = null);

public interface ITelegramManualRegistrySource
{
    IReadOnlyList<TelegramManualRegistryEntry> GetManuals();
}

public interface ITelegramManualFileBindingStore
{
    Task<TelegramManualFileBinding?> GetAsync(
        string manualId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramManualFileBinding>> ListAsync(
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        TelegramManualFileBinding binding,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(
        string manualId,
        CancellationToken cancellationToken = default);
}
