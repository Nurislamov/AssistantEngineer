using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed class EquipmentDiagnosticTelegramWebhookHandler : IEquipmentDiagnosticTelegramWebhookHandler
{
    private const string DiagnosticUnavailable = "Диагностика временно недоступна. Попробуйте позже.";
    private const string GenericCallbackUnavailable = "Действие временно недоступно.";
    private const string HistoryCallbackUnavailable = "История временно недоступна. Попробуйте позже.";
    private const string QueueCallbackUnavailable = "Очередь временно недоступна. Попробуйте позже.";

    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;
    private readonly EquipmentDiagnosticTelegramWebhookSecurityPolicy _securityPolicy;
    private readonly IEquipmentDiagnosticTelegramAdapter _adapter;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly ITelegramOperatorInboxService? _operatorInboxService;
    private readonly EquipmentDiagnosticTelegramOperationalCounters _counters;
    private readonly ILogger<EquipmentDiagnosticTelegramWebhookHandler> _logger;

    public EquipmentDiagnosticTelegramWebhookHandler(
        EquipmentDiagnosticTelegramWebhookOptions options,
        EquipmentDiagnosticTelegramWebhookSecurityPolicy securityPolicy,
        IEquipmentDiagnosticTelegramAdapter adapter,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient)
        : this(
            options,
            securityPolicy,
            adapter,
            outboundClient,
            null,
            new EquipmentDiagnosticTelegramOperationalCounters(),
            null)
    {
    }

    public EquipmentDiagnosticTelegramWebhookHandler(
        EquipmentDiagnosticTelegramWebhookOptions options,
        EquipmentDiagnosticTelegramWebhookSecurityPolicy securityPolicy,
        IEquipmentDiagnosticTelegramAdapter adapter,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        EquipmentDiagnosticTelegramOperationalCounters counters,
        ILogger<EquipmentDiagnosticTelegramWebhookHandler>? logger = null)
        : this(options, securityPolicy, adapter, outboundClient, null, counters, logger)
    {
    }

    public EquipmentDiagnosticTelegramWebhookHandler(
        EquipmentDiagnosticTelegramWebhookOptions options,
        EquipmentDiagnosticTelegramWebhookSecurityPolicy securityPolicy,
        IEquipmentDiagnosticTelegramAdapter adapter,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        ITelegramOperatorInboxService? operatorInboxService,
        EquipmentDiagnosticTelegramOperationalCounters counters,
        ILogger<EquipmentDiagnosticTelegramWebhookHandler>? logger = null)
    {
        _options = options;
        _securityPolicy = securityPolicy;
        _adapter = adapter;
        _outboundClient = outboundClient;
        _operatorInboxService = operatorInboxService;
        _counters = counters;
        _logger = logger ?? NullLogger<EquipmentDiagnosticTelegramWebhookHandler>.Instance;
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
        if (update.CallbackQuery is not null)
        {
            return await ProcessCallbackAsync(update, cancellationToken);
        }

        if (update.Message?.Chat is null ||
            (string.IsNullOrWhiteSpace(update.Message.Text) &&
             string.IsNullOrWhiteSpace(update.Message.Caption) &&
             string.IsNullOrWhiteSpace(update.Message.Contact?.PhoneNumber) &&
             update.Message.Document is null &&
             update.Message.Photo is not { Count: > 0 } &&
             update.Message.Video is null &&
             update.Message.Voice is null))
        {
            _counters.RecordInvalidUpdate();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, "Telegram update does not contain a supported message.");
        }

        var mappedUpdate = MapMessageUpdate(update);
        if (_operatorInboxService is not null &&
            (await _operatorInboxService.TryHandleOperatorCommandAsync(mappedUpdate, cancellationToken) ||
             await _operatorInboxService.TryHandleOperatorReplyAsync(mappedUpdate, cancellationToken)))
        {
            _counters.RecordProcessed();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Processed, "Telegram operator inbox update processed.");
        }

        if (IsConfiguredOperatorChat(mappedUpdate))
        {
            _counters.RecordIgnored();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Ignored, "Telegram operator group update ignored.");
        }

        var username = update.Message.From?.Username ?? update.Message.Chat.Username;
        EquipmentDiagnosticTelegramResponse adapterResponse;
        try
        {
            adapterResponse = await _adapter.HandleAsync(mappedUpdate, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Telegram diagnostic processing failed and fallback was attempted. UpdateId: {UpdateId}; ChatType: {ChatType}; ExceptionType: {ExceptionType}; ExceptionMessage: {ExceptionMessage}; Context: {Context}.",
                update.UpdateId,
                SafeChatType(update.Message.Chat.Type),
                exception.GetType().Name,
                TelegramSafeExceptionDetails.Message(exception),
                TelegramSafeExceptionDetails.Context(exception));

            var fallback = await _outboundClient.SendMessageAsync(
                update.Message.Chat.Id,
                DiagnosticUnavailable,
                parseMode: null,
                disableWebPagePreview: true,
                replyMarkup: null,
                cancellationToken: cancellationToken);
            if (!fallback.Succeeded)
            {
                _counters.RecordOutboundSendFailure();
                return Result(
                    EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed,
                    "Telegram diagnostic fallback send failed.");
            }

            _counters.RecordProcessed();
            return Result(
                EquipmentDiagnosticTelegramWebhookStatus.Processed,
                "Telegram diagnostic failure was handled safely.");
        }

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
            var outbound = await SendOutboundMessageAsync(adapterResponse.ChatId, message, cancellationToken);

            if (!outbound.Succeeded)
            {
                _counters.RecordOutboundSendFailure();
                return Result(EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed, "Telegram outbound send failed.");
            }
        }

        _counters.RecordProcessed();
        return Result(EquipmentDiagnosticTelegramWebhookStatus.Processed, "Telegram update processed.");
    }

    private EquipmentDiagnosticTelegramUpdate MapMessageUpdate(TelegramWebhookUpdateDto update)
    {
        var message = update.Message!;
        var username = message.From?.Username ?? message.Chat?.Username;
        return new EquipmentDiagnosticTelegramUpdate(
            update.UpdateId,
            message.Chat!.Id,
            username,
            message.Text ?? message.Caption,
            message.MessageId,
            message.Date is null
                ? null
                : DateTimeOffset.FromUnixTimeSeconds(message.Date.Value),
            message.From?.Id,
            message.From?.FirstName,
            message.From?.LastName,
            message.Contact?.PhoneNumber,
            message.Contact?.UserId,
            message.Chat.Type,
            DocumentFileId: message.Document?.FileId,
            DocumentFileName: message.Document?.FileName,
            DocumentMimeType: message.Document?.MimeType,
            DocumentFileSize: message.Document?.FileSize,
            ReplyToDocumentFileId: message.ReplyToMessage?.Document?.FileId,
            ReplyToDocumentFileName: message.ReplyToMessage?.Document?.FileName,
            ReplyToDocumentMimeType: message.ReplyToMessage?.Document?.MimeType,
            ReplyToDocumentFileSize: message.ReplyToMessage?.Document?.FileSize,
            DocumentFileUniqueId: message.Document?.FileUniqueId,
            ReplyToDocumentFileUniqueId: message.ReplyToMessage?.Document?.FileUniqueId,
            ReplyToMessageId: message.ReplyToMessage?.MessageId,
            HasPhoto: message.Photo is { Count: > 0 },
            HasVideo: message.Video is not null,
            HasVoice: message.Voice is not null);
    }

    private bool IsConfiguredOperatorChat(EquipmentDiagnosticTelegramUpdate update) =>
        _options.OperatorInbox.ChatId == update.ChatId &&
        (string.Equals(update.ChatType, "group", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(update.ChatType, "supergroup", StringComparison.OrdinalIgnoreCase));

    private async Task<EquipmentDiagnosticTelegramWebhookResult> ProcessCallbackAsync(
        TelegramWebhookUpdateDto update,
        CancellationToken cancellationToken)
    {
        var callback = update.CallbackQuery!;
        var answerAttempted = false;
        try
        {
            if (callback.Message?.Chat is null)
            {
                answerAttempted = true;
                await TryAnswerCallbackAsync(callback.Id, "Действие недоступно.", cancellationToken);
                _counters.RecordInvalidUpdate();
                return Result(
                    EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate,
                    "Telegram callback query does not contain a supported message.");
            }

            var adapterResponse = await _adapter.HandleAsync(
                new EquipmentDiagnosticTelegramUpdate(
                    update.UpdateId,
                    callback.Message.Chat.Id,
                    callback.From.Username,
                    Text: null,
                    callback.Message.MessageId,
                    ReceivedAt: null,
                    callback.From.Id,
                    callback.From.FirstName,
                    callback.From.LastName,
                    ContactPhoneNumber: null,
                    ContactUserId: null,
                    callback.Message.Chat.Type,
                    callback.Id,
                    callback.Data),
                cancellationToken);

            answerAttempted = true;
            await TryAnswerCallbackAsync(
                callback.Id,
                adapterResponse.CallbackAnswerText ?? "Готово",
                cancellationToken);
            if (adapterResponse.SuppressOutbound)
            {
                _counters.RecordProcessed();
                return Result(EquipmentDiagnosticTelegramWebhookStatus.Processed, "Telegram callback processed.");
            }

            return await SendAdapterResponseAsync(adapterResponse, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var action = SafeCallbackAction(callback.Data);
            _logger.LogWarning(
                "Telegram callback action failed and was handled safely. Action: {Action}; ExceptionType: {ExceptionType}.",
                action,
                exception.GetType().Name);

            if (!answerAttempted)
            {
                answerAttempted = true;
                await TryAnswerCallbackAsync(
                    callback.Id,
                    IsHistoryCallback(callback.Data)
                        ? HistoryCallbackUnavailable
                        : IsQueueCallback(callback.Data)
                            ? QueueCallbackUnavailable
                        : GenericCallbackUnavailable,
                    cancellationToken);
            }

            _counters.RecordProcessed();
            return Result(
                EquipmentDiagnosticTelegramWebhookStatus.Processed,
                "Telegram callback failure was handled safely.");
        }
    }

    private async Task TryAnswerCallbackAsync(
        string callbackQueryId,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _outboundClient.AnswerCallbackQueryAsync(
                callbackQueryId,
                text,
                cancellationToken: cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Telegram answerCallbackQuery failed. Action: callback-answer.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram answerCallbackQuery failed. Action: callback-answer; ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
        }
    }

    private static bool IsHistoryCallback(string? data) =>
        data?.StartsWith("sr:e:", StringComparison.Ordinal) == true;

    private static bool IsQueueCallback(string? data) =>
        data?.StartsWith("sq:", StringComparison.Ordinal) == true;

    private static string SafeCallbackAction(string? data)
    {
        if (IsHistoryCallback(data))
        {
            return "history";
        }
        if (IsQueueCallback(data))
        {
            return "queue";
        }
        if (data?.StartsWith("sr:", StringComparison.Ordinal) == true)
        {
            return "service-request";
        }
        if (data?.StartsWith("au:", StringComparison.Ordinal) == true)
        {
            return "admin-user";
        }
        return "unknown";
    }

    private async Task<EquipmentDiagnosticTelegramWebhookResult> SendAdapterResponseAsync(
        EquipmentDiagnosticTelegramResponse adapterResponse,
        CancellationToken cancellationToken)
    {
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
            var outbound = await SendOutboundMessageAsync(adapterResponse.ChatId, message, cancellationToken);

            if (!outbound.Succeeded)
            {
                _counters.RecordOutboundSendFailure();
                return Result(EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed, "Telegram outbound send failed.");
            }
        }

        _counters.RecordProcessed();
        return Result(EquipmentDiagnosticTelegramWebhookStatus.Processed, "Telegram update processed.");
    }

    private async Task<EquipmentDiagnosticTelegramOutboundResult> SendOutboundMessageAsync(
        long chatId,
        EquipmentDiagnosticTelegramOutboundMessage message,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(message.DocumentFileId))
        {
            return await _outboundClient.SendDocumentAsync(
                chatId,
                message.DocumentFileId,
                message.Text,
                message.ReplyMarkup,
                protectContent: message.ProtectContent,
                cancellationToken: cancellationToken);
        }

        if (message.EditMessageId is not null)
        {
            var edit = await _outboundClient.EditMessageTextAsync(
                chatId,
                message.EditMessageId.Value,
                message.Text,
                InlineOnly(message.ReplyMarkup),
                cancellationToken);
            if (edit.Succeeded)
            {
                return edit;
            }
        }

        return await _outboundClient.SendMessageAsync(
            chatId,
            message.Text,
            message.ParseMode,
            message.DisableWebPagePreview,
            message.ReplyMarkup,
            cancellationToken);
    }

    private static EquipmentDiagnosticTelegramReplyMarkup? InlineOnly(
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup) =>
        replyMarkup?.InlineKeyboard is { Count: > 0 }
            ? new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: replyMarkup.InlineKeyboard)
            : null;

    private static EquipmentDiagnosticTelegramWebhookResult Result(
        EquipmentDiagnosticTelegramWebhookStatus status,
        string message) => new(status, message);

    private static string SafeChatType(string? chatType) =>
        string.IsNullOrWhiteSpace(chatType) ? "unknown" : chatType;
}
