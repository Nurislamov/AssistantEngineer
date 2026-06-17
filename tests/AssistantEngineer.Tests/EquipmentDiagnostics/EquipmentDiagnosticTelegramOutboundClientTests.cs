using System.Net;
using System.Text.Json;
using AssistantEngineer.Api.Services.EquipmentDiagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Api.Services.OperationalDiagnostics;
using Microsoft.Extensions.Logging;
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
        Assert.False(payload.RootElement.TryGetProperty("parse_mode", out _));
        Assert.Equal("outbound-test-id", handler.CorrelationId);
        Assert.DoesNotContain("test-token-value", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageIncludesParseModeOnlyWhenNonEmpty()
    {
        var withoutParseMode = new CapturingHandler(HttpStatusCode.OK);
        var withParseMode = new CapturingHandler(HttpStatusCode.OK);

        await CreateClient(withoutParseMode, EnabledOptions())
            .SendMessageAsync(42, "Safe reply", " ", true);
        await CreateClient(withParseMode, EnabledOptions())
            .SendMessageAsync(42, "Safe reply", "MarkdownV2", true);

        using var omittedPayload = JsonDocument.Parse(withoutParseMode.Body!);
        using var includedPayload = JsonDocument.Parse(withParseMode.Body!);
        Assert.False(omittedPayload.RootElement.TryGetProperty("parse_mode", out _));
        Assert.Equal("MarkdownV2", includedPayload.RootElement.GetProperty("parse_mode").GetString());
    }

    [Fact]
    public async Task SendMessageLogsDoNotExposeSecretsChatIdOrText()
    {
        var logger = new CapturingLogger<EquipmentDiagnosticTelegramOutboundClient>();
        var client = CreateClient(new CapturingHandler(HttpStatusCode.BadRequest), EnabledOptions(), logger);

        await client.SendMessageAsync(42, "Sensitive message text", null, true);

        var logged = string.Join(Environment.NewLine, logger.Messages.Concat(logger.Scopes));
        Assert.DoesNotContain("test-token-value", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("42", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive message text", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("sendMessage", logged, StringComparison.Ordinal);
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
        EquipmentDiagnosticTelegramWebhookOptions options,
        ILogger<EquipmentDiagnosticTelegramOutboundClient>? logger = null) =>
        new(
            new HttpClient(handler),
            options,
            new OperationalCorrelationIdAccessor { CorrelationId = "outbound-test-id" },
            logger ?? NullLogger<EquipmentDiagnosticTelegramOutboundClient>.Instance);

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

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<string> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object>> values)
            {
                Scopes.Add(string.Join(";", values.Select(value => $"{value.Key}={value.Value}")));
            }
            else
            {
                Scopes.Add(state.ToString() ?? string.Empty);
            }

            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
