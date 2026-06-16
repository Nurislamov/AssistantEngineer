using System.Net.Http.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Api.Services.OperationalDiagnostics;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramOutboundClient : IEquipmentDiagnosticTelegramOutboundClient
{
    private readonly HttpClient _httpClient;
    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;
    private readonly IOperationalCorrelationIdAccessor _correlation;
    private readonly ILogger<EquipmentDiagnosticTelegramOutboundClient> _logger;

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
        var payload = new System.Collections.Generic.Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["disable_web_page_preview"] = disableWebPagePreview
        };
        if (!string.IsNullOrWhiteSpace(parseMode))
        {
            payload["parse_mode"] = parseMode;
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

    private static EquipmentDiagnosticTelegramOutboundResult Failed() =>
        new(false, "Telegram outbound send failed.");
}
