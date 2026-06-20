namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public interface IEquipmentDiagnosticTelegramProcessedMessageStore
{
    Task<bool> IsProcessedMessageAsync(
        long chatId,
        long messageId,
        CancellationToken cancellationToken = default);

    Task MarkProcessedMessageAsync(
        long chatId,
        long messageId,
        long updateId,
        CancellationToken cancellationToken = default);
}
