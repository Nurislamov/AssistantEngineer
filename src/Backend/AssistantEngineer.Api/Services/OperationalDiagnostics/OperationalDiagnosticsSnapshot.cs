namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public sealed record OperationalDiagnosticsSnapshot(
    string ApplicationName,
    string EnvironmentName,
    string Version,
    DateTimeOffset StartedAtUtc,
    long UptimeSeconds,
    bool CorrelationEnabled,
    string CorrelationHeaderName,
    EquipmentDiagnosticsOperationalSnapshot EquipmentDiagnostics);

public sealed record EquipmentDiagnosticsOperationalSnapshot(
    bool BotEndpointAvailable,
    string TelegramInboundMode,
    bool TelegramWebhookConfigured,
    bool TelegramWebhookEnabled,
    bool TelegramPollingConfigured,
    bool TelegramPollingEnabled,
    bool ChatIdDiscoveryEnabled,
    bool TelegramAllowlistConfigured,
    bool AllowedChatIdsConfigured,
    bool AllowedUsernamesConfigured,
    bool DeniedChatIdsConfigured,
    long WebhookUpdatesReceived,
    long WebhookUpdatesProcessed,
    long WebhookUpdatesIgnored,
    long WebhookUpdatesRejectedUnauthorized,
    long WebhookUpdatesRejectedSecret,
    long WebhookInvalidUpdates,
    long WebhookOutboundSendFailures);
