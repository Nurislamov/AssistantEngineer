namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed class EquipmentDiagnosticTelegramWebhookHandler : IEquipmentDiagnosticTelegramWebhookHandler
{
    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;
    private readonly EquipmentDiagnosticTelegramWebhookSecurityPolicy _securityPolicy;
    private readonly IEquipmentDiagnosticTelegramAdapter _adapter;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;

    public EquipmentDiagnosticTelegramWebhookHandler(
        EquipmentDiagnosticTelegramWebhookOptions options,
        EquipmentDiagnosticTelegramWebhookSecurityPolicy securityPolicy,
        IEquipmentDiagnosticTelegramAdapter adapter,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient)
    {
        _options = options;
        _securityPolicy = securityPolicy;
        _adapter = adapter;
        _outboundClient = outboundClient;
    }

    public async Task<EquipmentDiagnosticTelegramWebhookResult> HandleAsync(
        TelegramWebhookUpdateDto update,
        string? suppliedSecret,
        CancellationToken cancellationToken = default)
    {
        var security = _securityPolicy.Validate(_options, suppliedSecret);
        if (security.Status != EquipmentDiagnosticTelegramWebhookStatus.Processed)
        {
            return security;
        }

        if (update.Message?.Chat is null || string.IsNullOrWhiteSpace(update.Message.Text))
        {
            return Result(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, "Telegram update does not contain a supported text message.");
        }

        var username = update.Message.From?.Username ?? update.Message.Chat.Username;
        var adapterResponse = await _adapter.HandleAsync(
            new EquipmentDiagnosticTelegramUpdate(
                update.UpdateId,
                update.Message.Chat.Id,
                username,
                update.Message.Text,
                update.Message.MessageId,
                update.Message.Date is null
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(update.Message.Date.Value),
                update.Message.From?.Id),
            cancellationToken);

        if (adapterResponse.ResponseKind == EquipmentDiagnosticTelegramResponseKind.Ignored)
        {
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Ignored, "Telegram update was ignored by adapter policy.");
        }

        if (string.IsNullOrWhiteSpace(adapterResponse.Text))
        {
            return Result(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, "Telegram adapter produced no user-facing response.");
        }

        var outbound = await _outboundClient.SendMessageAsync(
            adapterResponse.ChatId,
            adapterResponse.Text,
            adapterResponse.ParseMode,
            adapterResponse.DisableWebPagePreview,
            cancellationToken);

        return outbound.Succeeded
            ? Result(EquipmentDiagnosticTelegramWebhookStatus.Processed, "Telegram update processed.")
            : Result(EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed, "Telegram outbound send failed.");
    }

    private static EquipmentDiagnosticTelegramWebhookResult Result(
        EquipmentDiagnosticTelegramWebhookStatus status,
        string message) => new(status, message);
}
