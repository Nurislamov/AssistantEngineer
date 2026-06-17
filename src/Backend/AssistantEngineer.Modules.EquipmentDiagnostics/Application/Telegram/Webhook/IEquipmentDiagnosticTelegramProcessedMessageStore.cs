namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public interface IEquipmentDiagnosticTelegramProcessedMessageStore
{
    Task<bool> TryMarkProcessedMessageAsync(
        long chatId,
        long messageId,
        long updateId,
        CancellationToken cancellationToken = default);
}
