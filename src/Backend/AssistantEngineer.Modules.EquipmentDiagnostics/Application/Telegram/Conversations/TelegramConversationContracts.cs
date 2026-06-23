using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;

public enum TelegramConversationState
{
    Idle,
    WaitingForBrand,
    WaitingForEquipmentType,
    WaitingForDisplayContext,
    WaitingForPhoneNumber,
    ShowingResult
}

public sealed class TelegramConversationSessionEntity
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public TelegramConversationState State { get; set; } = TelegramConversationState.Idle;
    public string? CurrentCode { get; set; }
    public string? SelectedManufacturer { get; set; }
    public string? SelectedEquipmentType { get; set; }
    public string? SelectedDisplayContext { get; set; }
    public string? CandidateOptionsJson { get; set; }
    public long? LastPromptMessageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed record TelegramConversationSessionSnapshot(
    long Id,
    long TelegramUserId,
    TelegramConversationState State,
    string? CurrentCode,
    string? SelectedManufacturer,
    string? SelectedEquipmentType,
    string? SelectedDisplayContext,
    string? CandidateOptionsJson,
    long? LastPromptMessageId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ExpiresAt);

public interface ITelegramConversationSessionStore
{
    Task<TelegramConversationSessionSnapshot?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<TelegramConversationSessionSnapshot> UpsertAsync(
        TelegramConversationSessionUpsert session,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);
}

public sealed record TelegramConversationSessionUpsert(
    long TelegramUserId,
    TelegramConversationState State,
    string? CurrentCode,
    string? SelectedManufacturer,
    string? SelectedEquipmentType,
    string? SelectedDisplayContext,
    string? CandidateOptionsJson,
    long? LastPromptMessageId,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ExpiresAt);

public sealed record TelegramDiagnosticConversationResult(
    bool Handled,
    EquipmentDiagnosticTelegramResponseKind ResponseKind,
    string Text,
    IReadOnlyList<string> Warnings,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? Messages = null);

public sealed record TelegramDiagnosticCandidate(
    string Manufacturer,
    string? Series,
    string? ModelCode,
    string Code,
    EquipmentDiagnostics.Domain.EquipmentCategory Category,
    string EquipmentType,
    EquipmentDiagnosticBotEquipmentSide EquipmentSide,
    EquipmentDiagnosticBotDisplayContext DisplayContext,
    string? MeaningGroupId = null);
