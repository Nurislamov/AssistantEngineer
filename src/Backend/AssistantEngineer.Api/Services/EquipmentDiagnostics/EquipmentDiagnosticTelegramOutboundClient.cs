using System.Net.Http.Json;
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
                return new EquipmentDiagnosticTelegramOutboundResult(true, "Telegram message sent.");
            }

            _logger.LogWarning("Telegram outbound send failed with non-success status code.");
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

            _logger.LogWarning("Telegram command menu synchronization failed with non-success status code.");
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

    private static EquipmentDiagnosticTelegramOutboundResult Failed() =>
        new(false, "Telegram outbound send failed.");

    private static EquipmentDiagnosticTelegramSetCommandsResult SetCommandsFailed() =>
        new(false, "Telegram command menu synchronization failed.");

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
}
