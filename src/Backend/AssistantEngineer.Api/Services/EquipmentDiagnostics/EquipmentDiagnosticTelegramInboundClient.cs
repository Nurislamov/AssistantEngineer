using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramInboundClient : IEquipmentDiagnosticTelegramInboundClient
{
    private readonly HttpClient _httpClient;
    private readonly EquipmentDiagnosticTelegramWebhookOptions _options;

    public EquipmentDiagnosticTelegramInboundClient(
        HttpClient httpClient,
        EquipmentDiagnosticTelegramWebhookOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<TelegramWebhookUpdateDto>> GetUpdatesAsync(
        long offset,
        int limit,
        int timeoutSeconds,
        IReadOnlyCollection<string> allowedUpdates,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            CreateEndpoint("getUpdates"),
            new
            {
                offset,
                limit,
                timeout = timeoutSeconds,
                allowed_updates = allowedUpdates
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("Telegram getUpdates returned a non-success status code.");
        }

        var body = await response.Content.ReadFromJsonAsync<TelegramApiResponse<IReadOnlyList<TelegramWebhookUpdateDto>>>(
            cancellationToken);
        if (body is not { Ok: true })
        {
            throw new InvalidOperationException("Telegram getUpdates returned an unsuccessful API response.");
        }

        return body.Result ?? [];
    }

    public async Task<EquipmentDiagnosticTelegramDeleteWebhookResult> DeleteWebhookAsync(
        bool dropPendingUpdates,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            CreateEndpoint("deleteWebhook"),
            new
            {
                drop_pending_updates = dropPendingUpdates
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new EquipmentDiagnosticTelegramDeleteWebhookResult(false, "Telegram deleteWebhook returned a non-success status code.");
        }

        var body = await response.Content.ReadFromJsonAsync<TelegramApiResponse<object>>(
            cancellationToken);
        return body is { Ok: true }
            ? new EquipmentDiagnosticTelegramDeleteWebhookResult(true, "Telegram webhook deleted.")
            : new EquipmentDiagnosticTelegramDeleteWebhookResult(false, "Telegram deleteWebhook returned an unsuccessful API response.");
    }

    private Uri CreateEndpoint(string methodName)
    {
        if (!Uri.TryCreate(_options.TelegramApiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Telegram API base URL is invalid.");
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            throw new InvalidOperationException("Telegram bot token is missing.");
        }

        return new Uri($"{baseUri.AbsoluteUri.TrimEnd('/')}/bot{_options.BotToken}/{methodName}");
    }

    private sealed record TelegramApiResponse<T>(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("result")] T? Result);
}
