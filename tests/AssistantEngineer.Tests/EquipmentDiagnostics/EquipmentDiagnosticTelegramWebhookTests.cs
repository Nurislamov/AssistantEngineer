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
    public async Task CallbackQueryIsRoutedAndAnsweredBeforeGroupResponse()
    {
        var adapter = new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "Заявка #1 взята в работу.");
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);
        var callbackUpdate = new TelegramWebhookUpdateDto(
            7,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                "callback-1",
                new TelegramWebhookUserDto(77, "engineer", "Иван", "Иванов"),
                new TelegramWebhookMessageDto(
                    8,
                    Text: null,
                    new TelegramWebhookChatDto(-1001, null, "supergroup"),
                    From: null,
                    Date: null),
                "sr:t:1"));

        var result = await handler.HandleAsync(callbackUpdate, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.AnswerCallbackCount);
        Assert.Equal("callback-1", outbound.CallbackQueryId);
        Assert.Equal(1, outbound.CallCount);
        Assert.NotNull(adapter.LastUpdate);
        Assert.Equal(-1001, adapter.LastUpdate.ChatId);
        Assert.Equal(77, adapter.LastUpdate.UserId);
        Assert.Equal("sr:t:1", adapter.LastUpdate.CallbackData);
        Assert.Equal("callback-1", adapter.LastUpdate.CallbackQueryId);
    }

    [Fact]
    public async Task CallbackWithoutMessageIsStillAnsweredAndRejectedSafely()
    {
        var adapter = new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "unused");
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);
        var callbackUpdate = new TelegramWebhookUpdateDto(
            7,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                "callback-2",
                new TelegramWebhookUserDto(77, "engineer"),
                Message: null,
                Data: "broken"));

        var result = await handler.HandleAsync(callbackUpdate, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate, result.Status);
        Assert.Equal(1, outbound.AnswerCallbackCount);
        Assert.Equal(0, adapter.CallCount);
        Assert.Equal(0, outbound.CallCount);
    }

    [Fact]
    public async Task SuppressedCallbackUsesAnswerCallbackQueryWithoutGroupMessage()
    {
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "internal result",
            callbackAnswerText: "Статус обновлён",
            suppressOutbound: true);
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);
        var callbackUpdate = new TelegramWebhookUpdateDto(
            7,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                "callback-3",
                new TelegramWebhookUserDto(77, "engineer"),
                new TelegramWebhookMessageDto(
                    8,
                    Text: null,
                    new TelegramWebhookChatDto(-1001, null, "supergroup"),
                    From: null,
                    Date: null),
                "sr:s:1"));

        var result = await handler.HandleAsync(callbackUpdate, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal("Статус обновлён", outbound.CallbackAnswerText);
        Assert.Equal(0, outbound.CallCount);
    }

    [Fact]
    public async Task HistoryCallbackIsAnsweredAndSendsCompactHistory()
    {
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "История заявки #4\n\n18.06.2026 22:36 — заявка создана",
            callbackAnswerText: "История загружена");
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);
        var callbackUpdate = new TelegramWebhookUpdateDto(
            8,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                "callback-history",
                new TelegramWebhookUserDto(77, "engineer"),
                new TelegramWebhookMessageDto(
                    9,
                    Text: null,
                    new TelegramWebhookChatDto(-1001, null, "supergroup"),
                    From: null,
                    Date: null),
                "sr:e:4"));

        var result = await handler.HandleAsync(callbackUpdate, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal("История загружена", outbound.CallbackAnswerText);
        Assert.Equal(1, outbound.CallCount);
        Assert.Contains("История заявки #4", outbound.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HistoryCallbackAdapterFailureIsAnsweredOnceAndHandledAsProcessed()
    {
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(
            EnabledOptions(),
            _policy,
            new ThrowingAdapter(),
            outbound);
        var callbackUpdate = CallbackUpdate("callback-history-failure", "sr:e:4");

        var result = await handler.HandleAsync(callbackUpdate, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.AnswerCallbackCount);
        Assert.Equal("История временно недоступна. Попробуйте позже.", outbound.CallbackAnswerText);
        Assert.Equal(0, outbound.CallCount);
    }

    [Fact]
    public async Task ServiceRequestCallbackAdapterFailureIsAnsweredOnceAndHandledAsProcessed()
    {
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(
            EnabledOptions(),
            _policy,
            new ThrowingAdapter(),
            outbound);
        var callbackUpdate = CallbackUpdate("callback-action-failure", "sr:t:4");

        var result = await handler.HandleAsync(callbackUpdate, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.AnswerCallbackCount);
        Assert.Equal("Действие временно недоступно.", outbound.CallbackAnswerText);
        Assert.Equal(0, outbound.CallCount);
    }

    [Fact]
    public async Task AnswerCallbackQueryFailureDoesNotEscapeOrRetry()
    {
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "internal result",
            callbackAnswerText: "Готово",
            suppressOutbound: true);
        var outbound = new FakeOutbound { ThrowOnAnswer = true };
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(
            CallbackUpdate("callback-answer-failure", "sr:s:4"),
            "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.AnswerCallbackCount);
        Assert.Equal(0, outbound.CallCount);
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

    private static TelegramWebhookUpdateDto CallbackUpdate(string callbackId, string data) =>
        new(
            9,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                callbackId,
                new TelegramWebhookUserDto(77, "engineer"),
                new TelegramWebhookMessageDto(
                    10,
                    Text: null,
                    new TelegramWebhookChatDto(-1001, null, "supergroup"),
                    From: null,
                    Date: null),
                data));

    private sealed class FakeAdapter(
        EquipmentDiagnosticTelegramResponseKind kind,
        string text,
        IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? messages = null,
        string? callbackAnswerText = null,
        bool suppressOutbound = false)
        : IEquipmentDiagnosticTelegramAdapter
    {
        public int CallCount { get; private set; }
        public EquipmentDiagnosticTelegramUpdate? LastUpdate { get; private set; }

        public Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
            EquipmentDiagnosticTelegramUpdate update,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastUpdate = update;
            return Task.FromResult(new EquipmentDiagnosticTelegramResponse(
                update.ChatId,
                text,
                kind,
                null,
                true,
                [],
                null,
                messages,
                callbackAnswerText,
                suppressOutbound));
        }
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public int CallCount { get; private set; }
        public string? Text { get; private set; }
        public List<string> Texts { get; } = [];
        public int AnswerCallbackCount { get; private set; }
        public string? CallbackQueryId { get; private set; }
        public string? CallbackAnswerText { get; private set; }
        public bool ThrowOnAnswer { get; set; }

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

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Commands set."));

        public Task<EquipmentDiagnosticTelegramOutboundResult> AnswerCallbackQueryAsync(
            string callbackQueryId,
            string? text = null,
            bool showAlert = false,
            CancellationToken cancellationToken = default)
        {
            AnswerCallbackCount++;
            CallbackQueryId = callbackQueryId;
            CallbackAnswerText = text;
            if (ThrowOnAnswer)
            {
                throw new InvalidOperationException("Telegram API unavailable.");
            }
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Answered."));
        }
    }

    private sealed class ThrowingAdapter : IEquipmentDiagnosticTelegramAdapter
    {
        public Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
            EquipmentDiagnosticTelegramUpdate update,
            CancellationToken cancellationToken = default) =>
            Task.FromException<EquipmentDiagnosticTelegramResponse>(
                new InvalidOperationException("database unavailable"));
    }
}
