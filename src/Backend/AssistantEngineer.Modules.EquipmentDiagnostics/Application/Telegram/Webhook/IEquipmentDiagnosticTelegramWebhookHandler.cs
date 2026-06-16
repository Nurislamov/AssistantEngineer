namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public interface IEquipmentDiagnosticTelegramWebhookHandler
{
    Task<EquipmentDiagnosticTelegramWebhookResult> HandleAsync(
        TelegramWebhookUpdateDto update,
        string? suppliedSecret,
        CancellationToken cancellationToken = default);

    Task<EquipmentDiagnosticTelegramWebhookResult> HandleTrustedAsync(
        TelegramWebhookUpdateDto update,
        CancellationToken cancellationToken = default);
}
