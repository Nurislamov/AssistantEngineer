using System.Reflection;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public sealed class OperationalDiagnosticsService : IOperationalDiagnosticsService
{
    private readonly DateTimeOffset _startedAtUtc;
    private readonly TimeProvider _timeProvider;
    private readonly IHostEnvironment _environment;
    private readonly IEquipmentDiagnosticBotFacade _botFacade;
    private readonly EquipmentDiagnosticTelegramWebhookOptions _telegramOptions;
    private readonly EquipmentDiagnosticTelegramWebhookSecurityPolicy _telegramSecurityPolicy;
    private readonly EquipmentDiagnosticTelegramOperationalCounters _telegramCounters;
    private readonly OperationalCorrelationOptions _correlationOptions;

    public OperationalDiagnosticsService(
        TimeProvider timeProvider,
        IHostEnvironment environment,
        IEquipmentDiagnosticBotFacade botFacade,
        EquipmentDiagnosticTelegramWebhookOptions telegramOptions,
        EquipmentDiagnosticTelegramWebhookSecurityPolicy telegramSecurityPolicy,
        EquipmentDiagnosticTelegramOperationalCounters telegramCounters,
        IOptions<OperationalCorrelationOptions> correlationOptions)
    {
        _timeProvider = timeProvider;
        _environment = environment;
        _botFacade = botFacade;
        _telegramOptions = telegramOptions;
        _telegramSecurityPolicy = telegramSecurityPolicy;
        _telegramCounters = telegramCounters;
        _correlationOptions = correlationOptions.Value;
        _startedAtUtc = timeProvider.GetUtcNow();
    }

    public OperationalDiagnosticsSnapshot GetSnapshot()
    {
        var now = _timeProvider.GetUtcNow();
        var counters = _telegramCounters.GetSnapshot();
        var telegramWebhookConfigured =
            !string.IsNullOrWhiteSpace(_telegramOptions.BotToken) &&
            _telegramSecurityPolicy.IsValidSecret(_telegramOptions.WebhookSecret) &&
            _telegramOptions.AllowedChatIds.Count > 0 &&
            !_telegramOptions.EnableChatIdDiscovery;
        var telegramPollingEnabled = _telegramOptions.IsPollingDeliveryEnabled();
        var telegramPollingConfigured =
            telegramPollingEnabled &&
            _telegramOptions.AllowedChatIds.Count > 0 &&
            !_telegramOptions.EnableChatIdDiscovery;

        return new OperationalDiagnosticsSnapshot(
            ApplicationName: _environment.ApplicationName,
            EnvironmentName: _environment.EnvironmentName,
            Version: Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
            StartedAtUtc: _startedAtUtc,
            UptimeSeconds: Math.Max(0, (long)(now - _startedAtUtc).TotalSeconds),
            CorrelationEnabled: _correlationOptions.Enabled,
            CorrelationHeaderName: _correlationOptions.HeaderName,
            EquipmentDiagnostics: new EquipmentDiagnosticsOperationalSnapshot(
                BotEndpointAvailable: _botFacade is not null,
                TelegramWebhookConfigured: telegramWebhookConfigured,
                TelegramWebhookEnabled: _telegramOptions.IsEnabled,
                TelegramPollingConfigured: telegramPollingConfigured,
                TelegramPollingEnabled: telegramPollingEnabled,
                ChatIdDiscoveryEnabled: _telegramOptions.EnableChatIdDiscovery,
                AllowedChatIdsConfigured: _telegramOptions.AllowedChatIds.Count > 0,
                DeniedChatIdsConfigured: _telegramOptions.DeniedChatIds.Count > 0,
                WebhookUpdatesReceived: counters.UpdatesReceived,
                WebhookUpdatesProcessed: counters.UpdatesProcessed,
                WebhookUpdatesIgnored: counters.UpdatesIgnored,
                WebhookUpdatesRejectedUnauthorized: counters.UpdatesRejectedUnauthorized,
                WebhookUpdatesRejectedSecret: counters.UpdatesRejectedSecret,
                WebhookInvalidUpdates: counters.InvalidUpdates,
                WebhookOutboundSendFailures: counters.OutboundSendFailures));
    }
}
