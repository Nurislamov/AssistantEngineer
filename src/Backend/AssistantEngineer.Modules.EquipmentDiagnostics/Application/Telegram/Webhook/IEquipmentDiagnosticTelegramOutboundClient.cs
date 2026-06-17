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
}
