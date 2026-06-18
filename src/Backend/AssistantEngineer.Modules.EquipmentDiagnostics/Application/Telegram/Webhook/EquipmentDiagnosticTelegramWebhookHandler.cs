namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed class EquipmentDiagnosticTelegramWebhookHandler : IEquipmentDiagnosticTelegramWebhookHandler
{
    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;
    private readonly EquipmentDiagnosticTelegramWebhookSecurityPolicy _securityPolicy;
    private readonly IEquipmentDiagnosticTelegramAdapter _adapter;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly EquipmentDiagnosticTelegramOperationalCounters _counters;

    public EquipmentDiagnosticTelegramWebhookHandler(
        EquipmentDiagnosticTelegramWebhookOptions options,
        EquipmentDiagnosticTelegramWebhookSecurityPolicy securityPolicy,
        IEquipmentDiagnosticTelegramAdapter adapter,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient)
        : this(options, securityPolicy, adapter, outboundClient, new EquipmentDiagnosticTelegramOperationalCounters())
    {
    }

    public EquipmentDiagnosticTelegramWebhookHandler(
        EquipmentDiagnosticTelegramWebhookOptions options,
        EquipmentDiagnosticTelegramWebhookSecurityPolicy securityPolicy,
        IEquipmentDiagnosticTelegramAdapter adapter,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        EquipmentDiagnosticTelegramOperationalCounters counters)
    {
        _options = options;
        _securityPolicy = securityPolicy;
        _adapter = adapter;
        _outboundClient = outboundClient;
        _counters = counters;
    }

    public async Task<EquipmentDiagnosticTelegramWebhookResult> HandleAsync(
        TelegramWebhookUpdateDto update,
        string? suppliedSecret,
        CancellationToken cancellationToken = default)
    {
        _counters.RecordReceived();
        var security = _securityPolicy.Validate(_options, suppliedSecret);
        if (security.Status != EquipmentDiagnosticTelegramWebhookStatus.Processed)
        {
            if (security.Status is EquipmentDiagnosticTelegramWebhookStatus.Unauthorized or
                EquipmentDiagnosticTelegramWebhookStatus.Rejected)
            {
                _counters.RecordRejectedSecret();
            }
            else
            {
                _counters.RecordIgnored();
            }
            return security;
        }

        return await ProcessAcceptedUpdateAsync(update, cancellationToken);
    }

    public async Task<EquipmentDiagnosticTelegramWebhookResult> HandleTrustedAsync(
        TelegramWebhookUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            _counters.RecordIgnored();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Disabled, "Telegram transport is disabled.");
        }

        _counters.RecordReceived();
        return await ProcessAcceptedUpdateAsync(update, cancellationToken);
    }

    private async Task<EquipmentDiagnosticTelegramWebhookResult> ProcessAcceptedUpdateAsync(
        TelegramWebhookUpdateDto update,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Chat is null ||
            (string.IsNullOrWhiteSpace(update.Message.Text) &&
             string.IsNullOrWhiteSpace(update.Message.Contact?.PhoneNumber)))
        {
            _counters.RecordInvalidUpdate();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, "Telegram update does not contain a supported message.");
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
                update.Message.From?.Id,
                update.Message.From?.FirstName,
                update.Message.From?.LastName,
                update.Message.Contact?.PhoneNumber,
                update.Message.Contact?.UserId,
                update.Message.Chat.Type),
            cancellationToken);

        if (adapterResponse.ResponseKind == EquipmentDiagnosticTelegramResponseKind.Ignored)
        {
            _counters.RecordIgnored();
            _counters.RecordRejectedUnauthorized();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Ignored, "Telegram update was ignored by adapter policy.");
        }

        if (adapterResponse.OutboundMessages.Count == 0 ||
            adapterResponse.OutboundMessages.Any(message => string.IsNullOrWhiteSpace(message.Text)))
        {
            _counters.RecordInvalidUpdate();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, "Telegram adapter produced no user-facing response.");
        }

        foreach (var message in adapterResponse.OutboundMessages)
        {
            var outbound = await _outboundClient.SendMessageAsync(
                adapterResponse.ChatId,
                message.Text,
                message.ParseMode,
                message.DisableWebPagePreview,
                message.ReplyMarkup,
                cancellationToken);

            if (!outbound.Succeeded)
            {
                _counters.RecordOutboundSendFailure();
                return Result(EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed, "Telegram outbound send failed.");
            }
        }

        _counters.RecordProcessed();
        return Result(EquipmentDiagnosticTelegramWebhookStatus.Processed, "Telegram update processed.");
    }

    private static EquipmentDiagnosticTelegramWebhookResult Result(
        EquipmentDiagnosticTelegramWebhookStatus status,
        string message) => new(status, message);
}
