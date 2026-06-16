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
    bool TelegramWebhookConfigured,
    bool TelegramWebhookEnabled,
    bool TelegramPollingConfigured,
    bool TelegramPollingEnabled,
    bool ChatIdDiscoveryEnabled,
    bool AllowedChatIdsConfigured,
    bool DeniedChatIdsConfigured,
    long WebhookUpdatesReceived,
    long WebhookUpdatesProcessed,
    long WebhookUpdatesIgnored,
    long WebhookUpdatesRejectedUnauthorized,
    long WebhookUpdatesRejectedSecret,
    long WebhookInvalidUpdates,
    long WebhookOutboundSendFailures);
