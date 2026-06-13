using System.Net;
using System.Text.Json;
using AssistantEngineer.Api.Services.EquipmentDiagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Api.Services.OperationalDiagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramOutboundClientTests
{
    [Fact]
    public async Task SendMessageUsesExpectedPayloadWithoutExposingTokenInResult()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);
        var client = CreateClient(handler, EnabledOptions());

        var result = await client.SendMessageAsync(42, "Safe reply", null, true);

        Assert.True(result.Succeeded);
        Assert.NotNull(handler.RequestUri);
        Assert.Contains("/bottest-token-value/sendMessage", handler.RequestUri.AbsolutePath, StringComparison.Ordinal);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal(42, payload.RootElement.GetProperty("chat_id").GetInt64());
        Assert.Equal("Safe reply", payload.RootElement.GetProperty("text").GetString());
        Assert.True(payload.RootElement.GetProperty("disable_web_page_preview").GetBoolean());
        Assert.Equal("outbound-test-id", handler.CorrelationId);
        Assert.DoesNotContain("test-token-value", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NonSuccessAndMissingTokenReturnSafeFailure()
    {
        var failed = await CreateClient(new CapturingHandler(HttpStatusCode.BadRequest), EnabledOptions())
            .SendMessageAsync(42, "Safe reply", null, true);
        var missingToken = await CreateClient(new CapturingHandler(HttpStatusCode.OK), EnabledOptions() with { BotToken = null })
            .SendMessageAsync(42, "Safe reply", null, true);

        Assert.False(failed.Succeeded);
        Assert.False(missingToken.Succeeded);
        Assert.Equal("Telegram outbound send failed.", failed.Message);
        Assert.Equal("Telegram outbound send failed.", missingToken.Message);
    }

    private static EquipmentDiagnosticTelegramOutboundClient CreateClient(
        HttpMessageHandler handler,
        EquipmentDiagnosticTelegramWebhookOptions options) =>
        new(
            new HttpClient(handler),
            options,
            new OperationalCorrelationIdAccessor { CorrelationId = "outbound-test-id" },
            NullLogger<EquipmentDiagnosticTelegramOutboundClient>.Instance);

    private static EquipmentDiagnosticTelegramWebhookOptions EnabledOptions() => new()
    {
        IsEnabled = true,
        BotToken = "test-token-value",
        TelegramApiBaseUrl = "https://api.telegram.org"
    };

    private sealed class CapturingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? Body { get; private set; }
        public string? CorrelationId { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            CorrelationId = request.Headers.GetValues("X-Correlation-ID").Single();
            return new HttpResponseMessage(statusCode);
        }
    }
}
