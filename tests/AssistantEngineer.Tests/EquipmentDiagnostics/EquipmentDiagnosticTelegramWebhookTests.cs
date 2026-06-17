using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramWebhookTests
{
    private readonly EquipmentDiagnosticTelegramWebhookSecurityPolicy _policy = new();

    [Fact]
    public void DisabledTransportDoesNotRequireTokenOrSecret()
    {
        var result = _policy.Validate(new EquipmentDiagnosticTelegramWebhookOptions(), null);

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Disabled, result.Status);
        Assert.DoesNotContain("token", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, EquipmentDiagnosticTelegramWebhookStatus.Rejected)]
    [InlineData("bad secret", EquipmentDiagnosticTelegramWebhookStatus.Rejected)]
    [InlineData("valid_secret", EquipmentDiagnosticTelegramWebhookStatus.Unauthorized)]
    public void EnabledTransportFailsClosedForInvalidOrMissingAuthentication(
        string? configuredSecret,
        EquipmentDiagnosticTelegramWebhookStatus expected)
    {
        var options = EnabledOptions() with { WebhookSecret = configuredSecret };

        var result = _policy.Validate(options, null);

        Assert.Equal(expected, result.Status);
        if (!string.IsNullOrEmpty(configuredSecret))
        {
            Assert.DoesNotContain(configuredSecret, result.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CorrectSecretIsAcceptedWithoutEchoingSecret()
    {
        var options = EnabledOptions();

        var result = _policy.Validate(options, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.DoesNotContain("test_webhook_secret", result.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("bad secret")]
    [InlineData("bad.secret")]
    [InlineData("")]
    public void InvalidSecretCharactersFailValidation(string secret)
    {
        Assert.False(_policy.IsValidSecret(secret));
    }

    [Fact]
    public async Task HandlerCallsAdapterAndOutboundForAcceptedTextUpdate()
    {
        var adapter = new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "Safe deterministic reply");
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(Update("Gree H5"), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, adapter.CallCount);
        Assert.Equal(1, outbound.CallCount);
        Assert.Equal("Safe deterministic reply", outbound.Text);
    }

    [Fact]
    public async Task HandlerSendsMultipleAdapterMessagesInOrder()
    {
        var messages = new[]
        {
            new EquipmentDiagnosticTelegramOutboundMessage("part 1"),
            new EquipmentDiagnosticTelegramOutboundMessage("part 2")
        };
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "part 1",
            messages);
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(Update("Gree H5"), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(["part 1", "part 2"], outbound.Texts);
    }

    [Fact]
    public async Task UnauthorizedAndIgnoredUpdatesDoNotSendOutbound()
    {
        var adapter = new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Ignored, string.Empty);
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var unauthorized = await handler.HandleAsync(Update("Gree H5"), "wrong_secret");
        var ignored = await handler.HandleAsync(Update("Gree H5"), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Unauthorized, unauthorized.Status);
        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Ignored, ignored.Status);
        Assert.Equal(1, adapter.CallCount);
        Assert.Equal(0, outbound.CallCount);
    }

    [Fact]
    public async Task MissingTextIsInvalidAndDoesNotCallAdapter()
    {
        var adapter = new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "unused");
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, new FakeOutbound());

        var result = await handler.HandleAsync(Update(null), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, result.Status);
        Assert.Equal(0, adapter.CallCount);
    }

    private static EquipmentDiagnosticTelegramWebhookOptions EnabledOptions() => new()
    {
        IsEnabled = true,
        WebhookSecret = "test_webhook_secret",
        BotToken = "test-token-value"
    };

    private static TelegramWebhookUpdateDto Update(string? text) =>
        new(1, new TelegramWebhookMessageDto(2, text, new TelegramWebhookChatDto(3, "operator"), null, 1_700_000_000));

    private sealed class FakeAdapter(
        EquipmentDiagnosticTelegramResponseKind kind,
        string text,
        IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? messages = null)
        : IEquipmentDiagnosticTelegramAdapter
    {
        public int CallCount { get; private set; }

        public Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
            EquipmentDiagnosticTelegramUpdate update,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new EquipmentDiagnosticTelegramResponse(
                update.ChatId, text, kind, null, true, [], null, messages));
        }
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public int CallCount { get; private set; }
        public string? Text { get; private set; }
        public List<string> Texts { get; } = [];

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Text = text;
            Texts.Add(text);
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent."));
        }
    }
}
