using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramCommandMenuStartupService : IHostedService
{
    private static readonly IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> GlobalCommands =
    [
        new("start", "начать"),
        new("new", "новый код ошибки"),
        new("phone", "указать номер телефона"),
        new("me", "мой доступ"),
        new("help", "помощь")
    ];

    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly ILogger<EquipmentDiagnosticTelegramCommandMenuStartupService> _logger;

    public EquipmentDiagnosticTelegramCommandMenuStartupService(
        EquipmentDiagnosticTelegramWebhookOptions options,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        ILogger<EquipmentDiagnosticTelegramCommandMenuStartupService> logger)
    {
        _options = options;
        _outboundClient = outboundClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldSync(_options))
        {
            _logger.LogInformation("Telegram command menu synchronization is disabled.");
            return;
        }

        try
        {
            var result = await _outboundClient.SetMyCommandsAsync(GlobalCommands, cancellationToken);
            if (result.Succeeded)
            {
                _logger.LogInformation("Telegram command menu synchronized.");
            }
            else
            {
                _logger.LogWarning("Telegram command menu synchronization failed; startup will continue.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram command menu synchronization failed; startup will continue. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static bool ShouldSync(EquipmentDiagnosticTelegramWebhookOptions options) =>
        options.IsEnabled &&
        options.Commands.SyncOnStartup &&
        !string.IsNullOrWhiteSpace(options.BotToken);
}
