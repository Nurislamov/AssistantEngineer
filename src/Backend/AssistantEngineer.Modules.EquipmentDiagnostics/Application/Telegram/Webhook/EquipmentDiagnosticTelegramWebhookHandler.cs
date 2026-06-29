using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    {
        _options = options;
        _securityPolicy = securityPolicy;
        _adapter = adapter;
        _outboundClient = outboundClient;
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
             update.Message.Document is null))
        {
            _counters.RecordInvalidUpdate();
            return Result(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, "Telegram update does not contain a supported message.");
        }

        var username = update.Message.From?.Username ?? update.Message.Chat.Username;
        EquipmentDiagnosticTelegramResponse adapterResponse;
        try
        {
            adapterResponse = await _adapter.HandleAsync(
                new EquipmentDiagnosticTelegramUpdate(
                    update.UpdateId,
                    update.Message.Chat.Id,
                    username,
                    update.Message.Text ?? update.Message.Caption,
                    update.Message.MessageId,
                    update.Message.Date is null
                        ? null
                        : DateTimeOffset.FromUnixTimeSeconds(update.Message.Date.Value),
                    update.Message.From?.Id,
                    update.Message.From?.FirstName,
                    update.Message.From?.LastName,
                    update.Message.Contact?.PhoneNumber,
                    update.Message.Contact?.UserId,
                    update.Message.Chat.Type,
                    DocumentFileId: update.Message.Document?.FileId,
                    DocumentFileName: update.Message.Document?.FileName,
                    DocumentMimeType: update.Message.Document?.MimeType,
                    DocumentFileSize: update.Message.Document?.FileSize,
                    ReplyToDocumentFileId: update.Message.ReplyToMessage?.Document?.FileId,
                    ReplyToDocumentFileName: update.Message.ReplyToMessage?.Document?.FileName,
                    ReplyToDocumentMimeType: update.Message.ReplyToMessage?.Document?.MimeType,
                    ReplyToDocumentFileSize: update.Message.ReplyToMessage?.Document?.FileSize,
                    DocumentFileUniqueId: update.Message.Document?.FileUniqueId,
                    ReplyToDocumentFileUniqueId: update.Message.ReplyToMessage?.Document?.FileUniqueId),
                cancellationToken);
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
