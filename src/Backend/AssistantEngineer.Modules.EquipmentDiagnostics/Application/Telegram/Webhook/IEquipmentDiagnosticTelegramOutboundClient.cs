using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public interface IEquipmentDiagnosticTelegramOutboundClient
{
    Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
        long chatId,
        string text,
        string? parseMode,
        bool disableWebPagePreview,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
        IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
        CancellationToken cancellationToken = default);

    Task<EquipmentDiagnosticTelegramOutboundResult> SendDocumentAsync(
        long chatId,
        string telegramFileId,
        string? caption = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        bool protectContent = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram document transport is not configured."));

    Task<EquipmentDiagnosticTelegramOutboundResult> SendPhotoAsync(
        long chatId,
        string telegramFileId,
        string? caption = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        bool protectContent = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram photo transport is not configured."));

    Task<EquipmentDiagnosticTelegramOutboundResult> SendVideoAsync(
        long chatId,
        string telegramFileId,
        string? caption = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        bool protectContent = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram video transport is not configured."));

    Task<EquipmentDiagnosticTelegramOutboundResult> CopyMessageAsync(
        long chatId,
        long fromChatId,
        long messageId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram copyMessage transport is not configured."));

    Task<EquipmentDiagnosticTelegramOutboundResult> AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram callback answer transport is not configured."));

    Task<EquipmentDiagnosticTelegramOutboundResult> EditMessageTextAsync(
        long chatId,
        long messageId,
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram message edit transport is not configured."));
}
