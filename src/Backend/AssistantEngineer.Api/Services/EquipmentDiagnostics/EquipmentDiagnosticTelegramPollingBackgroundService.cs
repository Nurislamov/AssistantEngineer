using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramPollingBackgroundService : BackgroundService
{
    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;
    private readonly IEquipmentDiagnosticTelegramInboundClient _inboundClient;
    private readonly IEquipmentDiagnosticTelegramWebhookHandler _handler;
    private readonly IEquipmentDiagnosticTelegramUpdateOffsetStore _offsetStore;
    private readonly IEquipmentDiagnosticTelegramProcessedMessageStore _processedMessageStore;
    private readonly ILogger<EquipmentDiagnosticTelegramPollingBackgroundService> _logger;

    public EquipmentDiagnosticTelegramPollingBackgroundService(
        EquipmentDiagnosticTelegramWebhookOptions options,
        IEquipmentDiagnosticTelegramInboundClient inboundClient,
        IEquipmentDiagnosticTelegramWebhookHandler handler,
        IEquipmentDiagnosticTelegramUpdateOffsetStore offsetStore,
        IEquipmentDiagnosticTelegramProcessedMessageStore processedMessageStore,
        ILogger<EquipmentDiagnosticTelegramPollingBackgroundService> logger)
    {
        _options = options;
        _inboundClient = inboundClient;
        _handler = handler;
        _offsetStore = offsetStore;
        _processedMessageStore = processedMessageStore;
        _logger = logger;
    }

    public static bool ShouldStart(EquipmentDiagnosticTelegramWebhookOptions options) =>
        options.IsPollingDeliveryEnabled();

    public async Task<long?> PollOnceAsync(
        long? lastProcessedUpdateId,
        CancellationToken cancellationToken = default)
    {
        var polling = NormalizePollingOptions(_options.Polling);
        var offset = (lastProcessedUpdateId ?? 0) + 1;
        var updates = await _inboundClient.GetUpdatesAsync(
            offset,
            polling.Limit,
            polling.TimeoutSeconds,
            _options.AllowedUpdates,
            cancellationToken);

        _logger.LogInformation(
            "Telegram polling getUpdates batch received {UpdateCount} updates.",
            updates.Count);

        var currentLastProcessed = lastProcessedUpdateId;
        foreach (var update in updates.OrderBy(item => item.UpdateId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation(
                "Telegram polling update received. UpdateId: {UpdateId}; ChatType: {ChatType}.",
                update.UpdateId,
                SafeChatType(update.Message?.Chat?.Type ?? update.CallbackQuery?.Message?.Chat?.Type));

            if (await IsDuplicateMessageAsync(update, cancellationToken))
            {
                _logger.LogInformation(
                    "Telegram polling duplicate message skipped. UpdateId: {UpdateId}; ChatType: {ChatType}.",
                    update.UpdateId,
                    SafeChatType(update.Message?.Chat?.Type ?? update.CallbackQuery?.Message?.Chat?.Type));
                await _offsetStore.SaveLastProcessedUpdateIdAsync(update.UpdateId, cancellationToken);
                currentLastProcessed = update.UpdateId;
                continue;
            }

            var result = await _handler.HandleTrustedAsync(update, cancellationToken);
            if (result.Status is EquipmentDiagnosticTelegramWebhookStatus.Processed or
                EquipmentDiagnosticTelegramWebhookStatus.Ignored)
            {
                _logger.LogInformation(
                    "Telegram polling update processed. UpdateId: {UpdateId}; Status: {Status}.",
                    update.UpdateId,
                    result.Status);
            }
            else
            {
                _logger.LogWarning(
                    "Telegram polling update completed with failure status. UpdateId: {UpdateId}; Status: {Status}.",
                    update.UpdateId,
                    result.Status);
            }

            await _offsetStore.SaveLastProcessedUpdateIdAsync(update.UpdateId, cancellationToken);
            currentLastProcessed = update.UpdateId;
        }

        return currentLastProcessed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!ShouldStart(_options))
        {
            _logger.LogInformation("Telegram polling delivery is disabled.");
            return;
        }

        var polling = NormalizePollingOptions(_options.Polling);
        _logger.LogInformation(
            "Telegram polling started. TimeoutSeconds: {TimeoutSeconds}; Limit: {Limit}.",
            polling.TimeoutSeconds,
            polling.Limit);

        if (_options.DeleteWebhookOnStartup)
        {
            await DeleteWebhookOnStartupAsync(stoppingToken);
        }

        var lastProcessedUpdateId = await _offsetStore.GetLastProcessedUpdateIdAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                lastProcessedUpdateId = await PollOnceAsync(lastProcessedUpdateId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    "Telegram polling batch failed; retrying after configured delay. ExceptionType: {ExceptionType}.",
                    exception.GetType().Name);
                lastProcessedUpdateId = await _offsetStore.GetLastProcessedUpdateIdAsync(stoppingToken);
                await DelayAfterErrorAsync(polling.DelayAfterErrorSeconds, stoppingToken);
            }
        }

        _logger.LogInformation("Telegram polling stopped.");
    }

    private async Task DeleteWebhookOnStartupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inboundClient.DeleteWebhookAsync(
                dropPendingUpdates: true,
                cancellationToken);
            if (result.Succeeded)
            {
                _logger.LogInformation("Telegram deleteWebhook on startup succeeded.");
            }
            else
            {
                _logger.LogWarning("Telegram deleteWebhook on startup failed.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram deleteWebhook on startup failed. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
        }
    }

    private static Task DelayAfterErrorAsync(int delayAfterErrorSeconds, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromSeconds(Math.Clamp(delayAfterErrorSeconds, 1, 300)), cancellationToken);

    private async Task<bool> IsDuplicateMessageAsync(
        TelegramWebhookUpdateDto update,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Chat is null)
        {
            return false;
        }

        var marked = await _processedMessageStore.TryMarkProcessedMessageAsync(
            update.Message.Chat.Id,
            update.Message.MessageId,
            update.UpdateId,
            cancellationToken);
        return !marked;
    }

    private static EquipmentDiagnosticTelegramPollingOptions NormalizePollingOptions(
        EquipmentDiagnosticTelegramPollingOptions options) =>
        options with
        {
            TimeoutSeconds = Math.Clamp(options.TimeoutSeconds, 1, 55),
            Limit = Math.Clamp(options.Limit, 1, 100),
            DelayAfterErrorSeconds = Math.Clamp(options.DelayAfterErrorSeconds, 1, 300)
        };

    private static string SafeChatType(string? chatType) =>
        string.IsNullOrWhiteSpace(chatType) ? "unknown" : chatType;
}
