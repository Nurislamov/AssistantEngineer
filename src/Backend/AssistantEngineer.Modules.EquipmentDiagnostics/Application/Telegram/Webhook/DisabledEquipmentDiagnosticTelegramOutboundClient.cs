using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed class DisabledEquipmentDiagnosticTelegramOutboundClient : IEquipmentDiagnosticTelegramOutboundClient
{
    public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
        long chatId,
        string text,
        string? parseMode,
        bool disableWebPagePreview,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram outbound transport is not configured."));

    public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
        IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(
            false,
            "Telegram outbound transport is not configured."));

    public Task<EquipmentDiagnosticTelegramOutboundResult> AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
            false,
            "Telegram callback answer transport is not configured."));
}
