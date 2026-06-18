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

    Task<EquipmentDiagnosticTelegramOutboundResult> AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram callback answer transport is not configured."));
}
