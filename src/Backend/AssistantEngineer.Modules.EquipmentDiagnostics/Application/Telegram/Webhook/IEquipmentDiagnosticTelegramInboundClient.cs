namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public interface IEquipmentDiagnosticTelegramInboundClient
{
    Task<IReadOnlyList<TelegramWebhookUpdateDto>> GetUpdatesAsync(
        long offset,
        int limit,
        int timeoutSeconds,
        IReadOnlyCollection<string> allowedUpdates,
        CancellationToken cancellationToken = default);

    Task<EquipmentDiagnosticTelegramDeleteWebhookResult> DeleteWebhookAsync(
        bool dropPendingUpdates,
        CancellationToken cancellationToken = default);
}
