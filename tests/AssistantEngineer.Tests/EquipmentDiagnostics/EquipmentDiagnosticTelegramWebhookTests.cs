using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.Logging;

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

    [Theory]
    [InlineData("group")]
    [InlineData("supergroup")]
    public async Task GroupResponsesStripPrivateReplyKeyboardAndKeepInlineActions(string chatType)
    {
        var unsafeMarkup = new EquipmentDiagnosticTelegramReplyMarkup(
            Keyboard:
            [
                [new EquipmentDiagnosticTelegramKeyboardButton("Поделиться номером", RequestContact: true)]
            ],
            ResizeKeyboard: true,
            InlineKeyboard:
            [
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("💬 Ответить", "sr:reply:13")]
            ]);
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Service request",
            [new EquipmentDiagnosticTelegramOutboundMessage("Service request", ReplyMarkup: unsafeMarkup)]);
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);
        var update = new TelegramWebhookUpdateDto(
            1,
            new TelegramWebhookMessageDto(
                2,
                "message",
                new TelegramWebhookChatDto(3, "operator", chatType),
                new TelegramWebhookUserDto(4, "operator"),
                1_700_000_000));

        var result = await handler.HandleAsync(update, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Null(outbound.ReplyMarkup?.Keyboard);
        var button = Assert.Single(Assert.Single(outbound.ReplyMarkup!.InlineKeyboard!));
        Assert.Equal("sr:reply:13", button.CallbackData);
    }

    [Fact]
    public async Task PrivateResponseKeepsContactRequestKeyboard()
    {
        var privateMarkup = new EquipmentDiagnosticTelegramReplyMarkup(
            Keyboard:
            [
                [new EquipmentDiagnosticTelegramKeyboardButton("Поделиться номером", RequestContact: true)]
            ],
            ResizeKeyboard: true);
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Private menu",
            [new EquipmentDiagnosticTelegramOutboundMessage("Private menu", ReplyMarkup: privateMarkup)]);
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);
        var update = new TelegramWebhookUpdateDto(
            1,
            new TelegramWebhookMessageDto(
                2,
                "/start",
                new TelegramWebhookChatDto(3, "operator", "private"),
                new TelegramWebhookUserDto(4, "operator"),
                1_700_000_000));

        var result = await handler.HandleAsync(update, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.True(outbound.ReplyMarkup!.Keyboard![0][0].RequestContact);
    }

    [Fact]
    public async Task TextDiagnosticFailureSendsSafeFallbackAndLogsSafeContext()
    {
        var outbound = new FakeOutbound();
        var logger = new CapturingLogger<EquipmentDiagnosticTelegramWebhookHandler>();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(
            EnabledOptions(),
            _policy,
            new ThrowingAdapter(
                "No embedded error knowledge JSON resources were found in production assembly. " +
                "token=secret-value phone=+998 90 123 45 67"),
            outbound,
            new EquipmentDiagnosticTelegramOperationalCounters(),
            logger);

        var result = await handler.HandleAsync(Update("Gree H5"), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.CallCount);
        Assert.Equal("Диагностика временно недоступна. Попробуйте позже.", outbound.Text);
        var log = Assert.Single(logger.Messages);
        Assert.Contains("InvalidOperationException", log, StringComparison.Ordinal);
        Assert.Contains("No embedded error knowledge JSON resources were found", log, StringComparison.Ordinal);
        Assert.Contains("Context:", log, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-value", log, StringComparison.Ordinal);
        Assert.DoesNotContain("+998", log, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree H5", log, StringComparison.Ordinal);
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
    public async Task HandlerSendsDocumentMessagesThroughOutboundDocumentMethod()
    {
        var messages = new[]
        {
            new EquipmentDiagnosticTelegramOutboundMessage(
                "Руководство: Service Manual for GMV6 v_2020.09",
                DocumentFileId: "telegram-file-id-gmv6",
                ProtectContent: true)
        };
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            messages[0].Text,
            messages);
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(Update("📘 Руководства"), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(0, outbound.CallCount);
        Assert.Equal(1, outbound.DocumentCallCount);
        Assert.Equal("telegram-file-id-gmv6", outbound.DocumentFileId);
        Assert.True(outbound.DocumentProtectContent);
        Assert.DoesNotContain("telegram-file-id-gmv6", outbound.DocumentCaption, StringComparison.OrdinalIgnoreCase);
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
    public async Task CallbackOutboundMessageWithEditMessageIdEditsInsteadOfSendingText()
    {
        var messages = new[]
        {
            new EquipmentDiagnosticTelegramOutboundMessage(
                "Updated library card",
                ReplyMarkup: new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
                [
                    [new EquipmentDiagnosticTelegramInlineKeyboardButton("Back", "lib:open")]
                ]),
                EditMessageId: 10)
        };
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Updated library card",
            messages,
            callbackAnswerText: "OK");
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(CallbackUpdate("callback-edit", "lib:brand:gree"), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.AnswerCallbackCount);
        Assert.Equal(1, outbound.EditCallCount);
        Assert.Equal(0, outbound.CallCount);
        Assert.Equal(-1001, outbound.EditChatId);
        Assert.Equal(10, outbound.EditMessageId);
        Assert.Equal("Updated library card", outbound.EditText);
        Assert.NotNull(outbound.EditReplyMarkup?.InlineKeyboard);
    }

    [Fact]
    public async Task CallbackEditFallsBackToSendMessageWhenTelegramEditFails()
    {
        var messages = new[]
        {
            new EquipmentDiagnosticTelegramOutboundMessage("Updated library card", EditMessageId: 10)
        };
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Updated library card",
            messages,
            callbackAnswerText: "OK");
        var outbound = new FakeOutbound { FailEdits = true };
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(CallbackUpdate("callback-edit-fallback", "lib:brand:gree"), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.EditCallCount);
        Assert.Equal(1, outbound.CallCount);
        Assert.Equal("Updated library card", outbound.Text);
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
    public async Task QueueFilterCallbackAnswersCallbackQueryWithoutGroupMessage()
    {
        var adapter = new FakeAdapter(
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Новые сервисные заявки",
            callbackAnswerText: "Очередь обновлена",
            suppressOutbound: true);
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(
            CallbackUpdate("callback-queue", "sq:n"),
            "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, outbound.AnswerCallbackCount);
        Assert.Equal("Очередь обновлена", outbound.CallbackAnswerText);
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
    public async Task VideoNoteUpdateIsAcceptedAndMappedSeparatelyFromVideo()
    {
        var adapter = new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "Safe deterministic reply");
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(VideoNoteUpdate(), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.NotNull(adapter.LastUpdate);
        Assert.True(adapter.LastUpdate.HasVideoNote);
        Assert.False(adapter.LastUpdate.HasVideo);
        Assert.Null(adapter.LastUpdate.Text);
        Assert.DoesNotContain("video-note-file-id-secret", outbound.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("video-note-unique-id-secret", outbound.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("audio")]
    [InlineData("location")]
    [InlineData("animation")]
    public async Task CommonMediaUpdatesAreAcceptedAndMapped(string mediaKind)
    {
        var adapter = new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "Safe deterministic reply");
        var outbound = new FakeOutbound();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(EnabledOptions(), _policy, adapter, outbound);

        var result = await handler.HandleAsync(CommonMediaUpdate(mediaKind), "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.NotNull(adapter.LastUpdate);
        Assert.Null(adapter.LastUpdate.Text);
        Assert.Equal(mediaKind == "audio", adapter.LastUpdate.HasAudio);
        Assert.Equal(mediaKind == "location", adapter.LastUpdate.HasLocation);
        Assert.Equal(mediaKind == "animation", adapter.LastUpdate.HasAnimation);
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

    private static TelegramWebhookUpdateDto VideoNoteUpdate() =>
        new(
            11,
            new TelegramWebhookMessageDto(
                MessageId: 12,
                Text: null,
                Chat: new TelegramWebhookChatDto(3, "operator", "private"),
                From: new TelegramWebhookUserDto(4, "operator"),
                Date: 1_700_000_000,
                VideoNote: new TelegramWebhookVideoNoteDto(
                    "video-note-file-id-secret",
                    "video-note-unique-id-secret",
                    Duration: 4,
                    Length: 240,
                    FileSize: 1024)));

    private static TelegramWebhookUpdateDto CommonMediaUpdate(string mediaKind)
    {
        var message = new TelegramWebhookMessageDto(
            MessageId: 13,
            Text: null,
            Chat: new TelegramWebhookChatDto(3, "operator", "private"),
            From: new TelegramWebhookUserDto(4, "operator"),
            Date: 1_700_000_000);

        message = mediaKind switch
        {
            "audio" => message with
            {
                Audio = new TelegramWebhookAudioDto(
                    "audio-file-id-secret",
                    "audio-unique-id-secret",
                    Duration: 9,
                    FileName: "reply.mp3")
            },
            "location" => message with
            {
                Location = new TelegramWebhookLocationDto(41.3111, 69.2797)
            },
            "animation" => message with
            {
                Animation = new TelegramWebhookAnimationDto(
                    "animation-file-id-secret",
                    "animation-unique-id-secret",
                    FileName: "reply.gif",
                    Duration: 2,
                    Width: 320,
                    Height: 240)
            },
            _ => message
        };

        return new TelegramWebhookUpdateDto(12, message);
    }

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
        public int DocumentCallCount { get; private set; }
        public string? DocumentFileId { get; private set; }
        public string? DocumentCaption { get; private set; }
        public bool DocumentProtectContent { get; private set; }
        public bool ThrowOnAnswer { get; set; }
        public bool FailEdits { get; set; }
        public int EditCallCount { get; private set; }
        public long? EditChatId { get; private set; }
        public long? EditMessageId { get; private set; }
        public string? EditText { get; private set; }
        public EquipmentDiagnosticTelegramReplyMarkup? EditReplyMarkup { get; private set; }
        public EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup { get; private set; }

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
            ReplyMarkup = replyMarkup;
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent."));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Commands set."));

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendDocumentAsync(
            long chatId,
            string telegramFileId,
            string? caption = null,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            bool protectContent = false,
            CancellationToken cancellationToken = default)
        {
            DocumentCallCount++;
            DocumentFileId = telegramFileId;
            DocumentCaption = caption;
            DocumentProtectContent = protectContent;
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Document sent."));
        }

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

        public Task<EquipmentDiagnosticTelegramOutboundResult> EditMessageTextAsync(
            long chatId,
            long messageId,
            string text,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            EditCallCount++;
            EditChatId = chatId;
            EditMessageId = messageId;
            EditText = text;
            EditReplyMarkup = replyMarkup;
            return Task.FromResult(FailEdits
                ? new EquipmentDiagnosticTelegramOutboundResult(false, "Edit failed.")
                : new EquipmentDiagnosticTelegramOutboundResult(true, "Edited.", messageId));
        }
    }

    private sealed class ThrowingAdapter(string message = "database unavailable") : IEquipmentDiagnosticTelegramAdapter
    {
        public Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
            EquipmentDiagnosticTelegramUpdate update,
            CancellationToken cancellationToken = default) =>
            Task.FromException<EquipmentDiagnosticTelegramResponse>(
                new InvalidOperationException(message));
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            NullScope.Instance;

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
