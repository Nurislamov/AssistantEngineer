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
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new
            {
                chat_id = chatId,
                text,
                disable_web_page_preview = disableWebPagePreview,
                parse_mode = parseMode
            })
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
            return response.IsSuccessStatusCode
                ? new EquipmentDiagnosticTelegramOutboundResult(true, "Telegram message sent.")
                : Failed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failed();
        }
        catch (HttpRequestException)
        {
            return Failed();
        }
    }

    private static EquipmentDiagnosticTelegramOutboundResult Failed() =>
        new(false, "Telegram outbound send failed.");
}
