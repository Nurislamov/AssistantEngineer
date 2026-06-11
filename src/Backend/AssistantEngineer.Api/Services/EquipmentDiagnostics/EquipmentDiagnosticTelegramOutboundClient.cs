using System.Net.Http.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramOutboundClient : IEquipmentDiagnosticTelegramOutboundClient
{
    private readonly HttpClient _httpClient;
    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;

    public EquipmentDiagnosticTelegramOutboundClient(
        HttpClient httpClient,
        EquipmentDiagnosticTelegramWebhookOptions options)
    {
        _httpClient = httpClient;
        _options = options;
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

        try
        {
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
