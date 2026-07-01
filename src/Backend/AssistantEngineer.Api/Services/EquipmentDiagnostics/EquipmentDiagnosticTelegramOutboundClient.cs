using System.Net.Http.Json;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Api.Services.OperationalDiagnostics;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramOutboundClient : IEquipmentDiagnosticTelegramOutboundClient
{
    private readonly HttpClient _httpClient;
    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;
    private readonly IOperationalCorrelationIdAccessor _correlation;
    private readonly ILogger<EquipmentDiagnosticTelegramOutboundClient> _logger;

    private string? BotToken => _options.BotToken;

    public EquipmentDiagnosticTelegramOutboundClient(
        HttpClient httpClient,
        EquipmentDiagnosticTelegramWebhookOptions options,
        IOperationalCorrelationIdAccessor correlation,
        ILogger<EquipmentDiagnosticTelegramOutboundClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _correlation = correlation;
        _logger = logger;
    }

    public async Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
        long chatId,
        string text,
        string? parseMode,
        bool disableWebPagePreview,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        replyMarkup = EquipmentDiagnosticTelegramReplyMarkupSafety.ForChatId(replyMarkup, chatId);
        if (!_options.IsEnabled || string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return Failed();
        }

        if (!Uri.TryCreate(_options.TelegramApiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return Failed();
        }

        var endpoint = new Uri(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/bot{_options.BotToken}/sendMessage");
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["disable_web_page_preview"] = disableWebPagePreview
        };
        if (!string.IsNullOrWhiteSpace(parseMode))
        {
            payload["parse_mode"] = parseMode;
        }
        if (replyMarkup is not null)
        {
            payload["reply_markup"] = ToTelegramReplyMarkup(replyMarkup);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(_correlation.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(
                OperationalCorrelationOptions.DefaultHeaderName,
                _correlation.CorrelationId);
        }

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["correlationId"] = _correlation.CorrelationId ?? "none",
                ["chatIdPresent"] = true
            });
            _logger.LogInformation("Sending Telegram response");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new EquipmentDiagnosticTelegramOutboundResult(
                    true,
                    "Telegram message sent.",
                    await ReadMessageIdAsync(response, cancellationToken));
            }

            await LogTelegramApiFailureAsync(response, "send message", cancellationToken);
            return Failed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Telegram outbound send timed out.");
            return Failed();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Telegram outbound send failed. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return Failed();
        }
    }

    public async Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
        IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
        CancellationToken cancellationToken = default)
    {
        var botToken = BotToken;
        if (!_options.IsEnabled || string.IsNullOrWhiteSpace(botToken))
        {
            return SetCommandsFailed();
        }

        if (!Uri.TryCreate(_options.TelegramApiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return SetCommandsFailed();
        }

        var endpoint = new Uri(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/bot{botToken}/setMyCommands");
        var payload = new
        {
            commands = commands.Select(command => new
            {
                command = command.Command,
                description = command.Description
            }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(_correlation.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(
                OperationalCorrelationOptions.DefaultHeaderName,
                _correlation.CorrelationId);
        }

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["correlationId"] = _correlation.CorrelationId ?? "none",
                ["commandCount"] = commands.Count
            });
            _logger.LogInformation("Synchronizing Telegram bot command menu.");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new EquipmentDiagnosticTelegramSetCommandsResult(true, "Telegram command menu synchronized.");
            }

            await LogTelegramApiFailureAsync(response, "set commands", cancellationToken);
            return SetCommandsFailed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Telegram command menu synchronization timed out.");
            return SetCommandsFailed();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Telegram command menu synchronization failed. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return SetCommandsFailed();
        }
    }

    public async Task<EquipmentDiagnosticTelegramOutboundResult> SendDocumentAsync(
        long chatId,
        string telegramFileId,
        string? caption = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        bool protectContent = false,
        CancellationToken cancellationToken = default)
    {
        return await SendFileByIdAsync(
            chatId,
            telegramFileId,
            "sendDocument",
            "document",
            "document",
            caption,
            replyMarkup,
            protectContent,
            cancellationToken);
    }

    public async Task<EquipmentDiagnosticTelegramOutboundResult> SendPhotoAsync(
        long chatId,
        string telegramFileId,
        string? caption = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        bool protectContent = false,
        CancellationToken cancellationToken = default)
    {
        return await SendFileByIdAsync(
            chatId,
            telegramFileId,
            "sendPhoto",
            "photo",
            "photo",
            caption,
            replyMarkup,
            protectContent,
            cancellationToken);
    }

    public async Task<EquipmentDiagnosticTelegramOutboundResult> SendVideoAsync(
        long chatId,
        string telegramFileId,
        string? caption = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        bool protectContent = false,
        CancellationToken cancellationToken = default)
    {
        return await SendFileByIdAsync(
            chatId,
            telegramFileId,
            "sendVideo",
            "video",
            "video",
            caption,
            replyMarkup,
            protectContent,
            cancellationToken);
    }

    private async Task<EquipmentDiagnosticTelegramOutboundResult> SendFileByIdAsync(
        long chatId,
        string telegramFileId,
        string telegramMethod,
        string payloadField,
        string logName,
        string? caption = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        bool protectContent = false,
        CancellationToken cancellationToken = default)
    {
        replyMarkup = EquipmentDiagnosticTelegramReplyMarkupSafety.ForChatId(replyMarkup, chatId);
        if (!_options.IsEnabled || string.IsNullOrWhiteSpace(BotToken) ||
            string.IsNullOrWhiteSpace(telegramFileId) ||
            !Uri.TryCreate(_options.TelegramApiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return Failed();
        }

        var endpoint = new Uri(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/bot{BotToken}/{telegramMethod}");
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            [payloadField] = telegramFileId
        };
        if (protectContent)
        {
            payload["protect_content"] = true;
        }
        if (!string.IsNullOrWhiteSpace(caption))
        {
            payload["caption"] = caption;
        }
        if (replyMarkup is not null)
        {
            payload["reply_markup"] = ToTelegramReplyMarkup(replyMarkup);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(_correlation.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(
                OperationalCorrelationOptions.DefaultHeaderName,
                _correlation.CorrelationId);
        }

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["correlationId"] = _correlation.CorrelationId ?? "none",
                ["telegramFileIdPresent"] = true
            });
            _logger.LogInformation("Sending Telegram {TelegramFileKind}.", logName);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new EquipmentDiagnosticTelegramOutboundResult(
                    true,
                    $"Telegram {logName} sent.",
                    await ReadMessageIdAsync(response, cancellationToken));
            }

            await LogTelegramApiFailureAsync(response, $"send {logName}", cancellationToken);
            return Failed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Telegram {TelegramFileKind} send timed out.", logName);
            return Failed();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Telegram {TelegramFileKind} send failed. ExceptionType: {ExceptionType}.",
                logName,
                exception.GetType().Name);
            return Failed();
        }
    }

    public async Task<EquipmentDiagnosticTelegramOutboundResult> CopyMessageAsync(
        long chatId,
        long fromChatId,
        long messageId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled || string.IsNullOrWhiteSpace(BotToken) ||
            !Uri.TryCreate(_options.TelegramApiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return Failed();
        }

        var endpoint = new Uri(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/bot{BotToken}/copyMessage");
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["from_chat_id"] = fromChatId,
            ["message_id"] = messageId
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(_correlation.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(
                OperationalCorrelationOptions.DefaultHeaderName,
                _correlation.CorrelationId);
        }

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["correlationId"] = _correlation.CorrelationId ?? "none",
                ["copyMessage"] = true
            });
            _logger.LogInformation("Copying Telegram message.");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new EquipmentDiagnosticTelegramOutboundResult(
                    true,
                    "Telegram message copied.",
                    await ReadMessageIdAsync(response, cancellationToken));
            }

            await LogTelegramApiFailureAsync(response, "copy message", cancellationToken);
            return Failed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Telegram copyMessage timed out.");
            return Failed();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Telegram copyMessage failed. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return Failed();
        }
    }

    public async Task<EquipmentDiagnosticTelegramOutboundResult> AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled || string.IsNullOrWhiteSpace(BotToken) ||
            string.IsNullOrWhiteSpace(callbackQueryId) ||
            !Uri.TryCreate(_options.TelegramApiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return Failed();
        }

        var endpoint = new Uri(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/bot{BotToken}/answerCallbackQuery");
        var payload = new Dictionary<string, object?>
        {
            ["callback_query_id"] = callbackQueryId,
            ["show_alert"] = showAlert
        };
        if (!string.IsNullOrWhiteSpace(text))
        {
            payload["text"] = text;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(_correlation.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(
                OperationalCorrelationOptions.DefaultHeaderName,
                _correlation.CorrelationId);
        }

        try
        {
            _logger.LogInformation("Answering Telegram callback query.");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new EquipmentDiagnosticTelegramOutboundResult(true, "Telegram callback query answered.");
            }

            await LogTelegramApiFailureAsync(response, "answer callback query", cancellationToken);
            return Failed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Telegram callback query answer timed out.");
            return Failed();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Telegram callback query answer failed. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return Failed();
        }
    }

    public async Task<EquipmentDiagnosticTelegramOutboundResult> EditMessageTextAsync(
        long chatId,
        long messageId,
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        replyMarkup = EquipmentDiagnosticTelegramReplyMarkupSafety.ForChatId(replyMarkup, chatId);
        if (!_options.IsEnabled || string.IsNullOrWhiteSpace(BotToken) ||
            !Uri.TryCreate(_options.TelegramApiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return Failed();
        }

        var endpoint = new Uri(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/bot{BotToken}/editMessageText");
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["message_id"] = messageId,
            ["text"] = text,
            ["disable_web_page_preview"] = true
        };
        if (replyMarkup is not null)
        {
            payload["reply_markup"] = ToTelegramReplyMarkup(replyMarkup);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(_correlation.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(
                OperationalCorrelationOptions.DefaultHeaderName,
                _correlation.CorrelationId);
        }

        try
        {
            _logger.LogInformation("Editing Telegram message.");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new EquipmentDiagnosticTelegramOutboundResult(
                    true,
                    "Telegram message edited.",
                    await ReadMessageIdAsync(response, cancellationToken) ?? messageId);
            }
            if (await IsMessageNotModifiedAsync(response, cancellationToken))
            {
                return new EquipmentDiagnosticTelegramOutboundResult(
                    true,
                    "Telegram message is already current.",
                    messageId);
            }

            await LogTelegramApiFailureAsync(response, "edit message", cancellationToken);
            return Failed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Telegram message edit timed out.");
            return Failed();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Telegram message edit failed. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return Failed();
        }
    }

    private async Task LogTelegramApiFailureAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var error = await ReadTelegramApiErrorAsync(response, cancellationToken);
        _logger.LogWarning(
            "Telegram {Operation} failed with non-success status code. StatusCode: {StatusCode}. ErrorDescription: {ErrorDescription}. ErrorBody: {ErrorBody}.",
            operation,
            (int)response.StatusCode,
            error.Description,
            error.Body);
    }

    private async Task<TelegramApiErrorLog> ReadTelegramApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = string.Empty;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new TelegramApiErrorLog("unavailable", "unavailable");
        }

        var description = "none";
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("description", out var value) &&
                    value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    description = value.GetString()!;
                }
            }
            catch (JsonException)
            {
                description = "unparseable";
            }
        }

        return new TelegramApiErrorLog(
            SanitizeTelegramApiErrorText(description),
            SanitizeTelegramApiErrorText(body));
    }

    private string SanitizeTelegramApiErrorText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "empty";
        }

        var sanitized = value;
        if (!string.IsNullOrWhiteSpace(BotToken))
        {
            sanitized = sanitized.Replace(BotToken, "[redacted]", StringComparison.Ordinal);
        }

        sanitized = sanitized.Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        const int maxLength = 512;
        return sanitized.Length <= maxLength
            ? sanitized
            : sanitized[..maxLength] + "...";
    }

    private static EquipmentDiagnosticTelegramOutboundResult Failed() =>
        new(false, "Telegram outbound send failed.");

    private static EquipmentDiagnosticTelegramSetCommandsResult SetCommandsFailed() =>
        new(false, "Telegram command menu synchronization failed.");

    private static async Task<long?> ReadMessageIdAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return document.RootElement.TryGetProperty("result", out var result) &&
                result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("message_id", out var messageId) &&
                messageId.TryGetInt64(out var value)
                    ? value
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<bool> IsMessageNotModifiedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            return document.RootElement.TryGetProperty("description", out var description) &&
                description.GetString()?.Contains("message is not modified", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static Dictionary<string, object?> ToTelegramReplyMarkup(
        EquipmentDiagnosticTelegramReplyMarkup replyMarkup)
    {
        var payload = new Dictionary<string, object?>();
        if (replyMarkup.Keyboard is { Count: > 0 })
        {
            payload["keyboard"] = replyMarkup.Keyboard
                .Select(row => row
                    .Select(button => new Dictionary<string, object?>
                    {
                        ["text"] = button.Text,
                        ["request_contact"] = button.RequestContact
                    })
                    .ToArray())
                .ToArray();
        }
        if (replyMarkup.InlineKeyboard is { Count: > 0 })
        {
            payload["inline_keyboard"] = replyMarkup.InlineKeyboard
                .Select(row => row
                    .Select(button => new Dictionary<string, object?>
                    {
                        ["text"] = button.Text,
                        ["callback_data"] = button.CallbackData
                    })
                    .ToArray())
                .ToArray();
        }

        if (replyMarkup.ResizeKeyboard is not null)
        {
            payload["resize_keyboard"] = replyMarkup.ResizeKeyboard.Value;
        }
        if (replyMarkup.OneTimeKeyboard is not null)
        {
            payload["one_time_keyboard"] = replyMarkup.OneTimeKeyboard.Value;
        }
        if (replyMarkup.RemoveKeyboard is not null)
        {
            payload["remove_keyboard"] = replyMarkup.RemoveKeyboard.Value;
        }

        return payload;
    }

    private sealed record TelegramApiErrorLog(
        string Description,
        string Body);
}
