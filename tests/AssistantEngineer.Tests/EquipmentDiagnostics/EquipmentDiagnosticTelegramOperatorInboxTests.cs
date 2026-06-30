using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramOperatorInboxTests
{
    [Fact]
    public async Task OperatorChatIdCommandWorksOnlyForOwnerInGroup()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        var ownerHandled = await service.TryHandleOperatorCommandAsync(GroupUpdate("/operator_chat_id", userId: 777));
        var nonOwnerHandled = await service.TryHandleOperatorCommandAsync(GroupUpdate("/operator_chat_id", userId: 888));
        var privateHandled = await service.TryHandleOperatorCommandAsync(
            new EquipmentDiagnosticTelegramUpdate(3, 7, "owner", "/operator_chat_id", UserId: 777, ChatType: "private"));

        Assert.True(ownerHandled);
        Assert.True(nonOwnerHandled);
        Assert.True(privateHandled);
        Assert.Contains("TELEGRAM_OPERATOR_CHAT_ID=-100500", outbound.Messages[0].Text, StringComparison.Ordinal);
        Assert.Equal("Доступ ограничен.", outbound.Messages[1].Text);
        Assert.Equal("Команду нужно отправить в operator group chat.", outbound.Messages[2].Text);
    }

    [Fact]
    public async Task MirroredUserTextCanBeAnsweredByOwnerReply()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var access = await ResolveAccessAsync(provider, UserUpdate("Нужна помощь специалиста"));
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        await service.MirrorUserMessageAsync(UserUpdate("Нужна помощь специалиста"), access);
        var replyHandled = await service.TryHandleOperatorReplyAsync(
            GroupUpdate("Проверьте питание наружного блока.", userId: 777, messageId: 500, replyToMessageId: 101));

        Assert.True(replyHandled);
        Assert.Equal(-100500, outbound.Messages[0].ChatId);
        Assert.Contains("📩 Обращение #1", outbound.Messages[0].Text, StringComparison.Ordinal);
        Assert.Contains("Пользователь:", outbound.Messages[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("file_id", outbound.Messages[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(20, outbound.Messages[1].ChatId);
        Assert.Equal("Ответ специалиста:\nПроверьте питание наружного блока.", outbound.Messages[1].Text);
        Assert.Equal(-100500, outbound.Messages[2].ChatId);
        Assert.Equal("Ответ отправлен пользователю.", outbound.Messages[2].Text);
    }

    [Fact]
    public async Task OwnerTextReplyPreservesUrlAndPersistsTextKind()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var access = await ResolveAccessAsync(provider, UserUpdate("Need help with a link"));
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        await service.MirrorUserMessageAsync(UserUpdate("Need help with a link"), access);
        var handled = await service.TryHandleOperatorReplyAsync(
            GroupUpdate("Open https://example.com/manual?id=42", userId: 777, messageId: 500, replyToMessageId: 101));
        var storedReply = await provider.GetRequiredService<ITelegramOperatorInboxStore>()
            .GetByOperatorMessageAsync(-100500, 500);

        Assert.True(handled);
        Assert.Contains("https://example.com/manual?id=42", outbound.Messages[1].Text, StringComparison.Ordinal);
        Assert.NotNull(storedReply);
        Assert.Equal(TelegramOperatorInboxMessageKind.Text, storedReply.MessageKind);
    }

    [Theory]
    [InlineData(TelegramOperatorInboxMessageKind.Document)]
    [InlineData(TelegramOperatorInboxMessageKind.Photo)]
    [InlineData(TelegramOperatorInboxMessageKind.Video)]
    [InlineData(TelegramOperatorInboxMessageKind.VideoNote)]
    [InlineData(TelegramOperatorInboxMessageKind.Voice)]
    [InlineData(TelegramOperatorInboxMessageKind.Audio)]
    [InlineData(TelegramOperatorInboxMessageKind.Contact)]
    [InlineData(TelegramOperatorInboxMessageKind.Location)]
    [InlineData(TelegramOperatorInboxMessageKind.Animation)]
    public async Task OwnerCopiedReplyKindsAreCopiedToUserAndPersisted(TelegramOperatorInboxMessageKind kind)
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var access = await ResolveAccessAsync(provider, UserUpdate("Need media reply"));
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        await service.MirrorUserMessageAsync(UserUpdate("Need media reply"), access);
        var handled = await service.TryHandleOperatorReplyAsync(
            GroupMediaReply(kind, messageId: 600, replyToMessageId: 101));
        var storedReply = await provider.GetRequiredService<ITelegramOperatorInboxStore>()
            .GetByOperatorMessageAsync(-100500, 600);

        Assert.True(handled);
        Assert.Single(outbound.CopyMessages);
        Assert.Equal((20, -100500, 600), outbound.CopyMessages[0]);
        Assert.NotNull(storedReply);
        Assert.Equal(kind, storedReply.MessageKind);
        Assert.Equal(-100500, outbound.Messages[1].ChatId);
        Assert.Contains("Ответ отправлен пользователю", outbound.Messages[1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OwnerMediaReplyToCopiedUserMediaWorks()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var access = await ResolveAccessAsync(provider, DocumentUpdate("telegram-file-id-secret"));
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        await service.MirrorUserMessageAsync(DocumentUpdate("telegram-file-id-secret"), access);
        var handled = await service.TryHandleOperatorReplyAsync(
            GroupMediaReply(TelegramOperatorInboxMessageKind.Photo, messageId: 700, replyToMessageId: 102));

        Assert.True(handled);
        Assert.Equal(2, outbound.CopyMessages.Count);
        Assert.Equal((-100500, 20, 55), outbound.CopyMessages[0]);
        Assert.Equal((20, -100500, 700), outbound.CopyMessages[1]);
    }

    [Fact]
    public async Task OwnerMediaReplyCopyFailureGetsSafeMessage()
    {
        var outbound = new FakeOutbound { FailCopiesToUser = true };
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var access = await ResolveAccessAsync(provider, UserUpdate("Need media reply"));
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        await service.MirrorUserMessageAsync(UserUpdate("Need media reply"), access);
        var handled = await service.TryHandleOperatorReplyAsync(
            GroupMediaReply(TelegramOperatorInboxMessageKind.Document, messageId: 600, replyToMessageId: 101));

        Assert.True(handled);
        Assert.Single(outbound.CopyMessages);
        Assert.Contains("Не удалось отправить вложение пользователю.", outbound.Messages[1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MediaMirrorUsesCopyMessageOnlyToOperatorGroupAndKeepsFileIdOutOfCard()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        var access = await ResolveAccessAsync(provider, DocumentUpdate("telegram-file-id-secret"));
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        await service.MirrorUserMessageAsync(DocumentUpdate("telegram-file-id-secret"), access);

        Assert.Single(outbound.CopyMessages);
        Assert.Equal((-100500, 20, 55), outbound.CopyMessages[0]);
        Assert.Contains("Сообщение:", outbound.Messages[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-file-id-secret", outbound.Messages[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VideoNoteMirrorUsesSafeCardAndCanBeAnsweredByCardOrCopiedMedia()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var access = await ResolveAccessAsync(provider, VideoNoteUpdate());
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        var mirrored = await service.MirrorUserMessageAsync(VideoNoteUpdate(), access);
        var storedCard = await provider.GetRequiredService<ITelegramOperatorInboxStore>()
            .GetByOperatorMessageAsync(-100500, 101);
        var cardReplyHandled = await service.TryHandleOperatorReplyAsync(
            GroupUpdate("Ответ по кружочку.", userId: 777, messageId: 501, replyToMessageId: 101));
        var copiedReplyHandled = await service.TryHandleOperatorReplyAsync(
            GroupUpdate("Еще ответ по кружочку.", userId: 777, messageId: 502, replyToMessageId: 102));

        Assert.True(mirrored);
        Assert.NotNull(storedCard);
        Assert.Equal(TelegramOperatorInboxMessageKind.VideoNote, storedCard.MessageKind);
        Assert.Single(outbound.CopyMessages);
        Assert.Equal((-100500, 20, 55), outbound.CopyMessages[0]);
        Assert.Contains("[Видео-кружок]", outbound.Messages[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("file_id", outbound.Messages[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(cardReplyHandled);
        Assert.True(copiedReplyHandled);
        Assert.Contains("Ответ по кружочку.", outbound.Messages[1].Text, StringComparison.Ordinal);
        Assert.Contains("Еще ответ по кружочку.", outbound.Messages[3].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdapterReturnsTransferredResponseForMirroredVideoNote()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(VideoNoteUpdate());

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Equal("Сообщение передано специалисту.", response.Text);
        Assert.Single(outbound.CopyMessages);
        Assert.Equal((-100500, 20, 55), outbound.CopyMessages[0]);
        Assert.Contains("[Видео-кружок]", outbound.Messages.Single().Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownOperatorReplyTargetDoesNotCallAdapterOrUserChat()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var service = provider.GetRequiredService<ITelegramOperatorInboxService>();

        var handled = await service.TryHandleOperatorReplyAsync(
            GroupUpdate("Ответ", userId: 777, messageId: 500, replyToMessageId: 9999));

        Assert.True(handled);
        var message = Assert.Single(outbound.Messages);
        Assert.Equal(-100500, message.ChatId);
        Assert.Equal("Не удалось определить получателя. Ответьте reply-сообщением на карточку обращения.", message.Text);
    }

    [Fact]
    public async Task AdapterMirrorsUnsupportedFreeTextButNotCommands()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(UserUpdate("Просто вопрос человеку"));
        await adapter.HandleAsync(UserUpdate("/start"));

        Assert.Single(outbound.Messages);
        Assert.Equal(-100500, outbound.Messages[0].ChatId);
        Assert.Contains("Просто вопрос человеку", outbound.Messages[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WebhookHandlesOperatorGroupBeforeDiagnosticAdapter()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound);
        await CreateUserAsync(provider, chatId: 7, userId: 777, TelegramUserRole.Owner);
        var adapter = new FakeAdapter();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(
            new EquipmentDiagnosticTelegramWebhookOptions
            {
                IsEnabled = true,
                BotToken = "test-token",
                OperatorInbox = new TelegramOperatorInboxOptions
                {
                    Enabled = true,
                    ChatId = -100500
                }
            },
            new EquipmentDiagnosticTelegramWebhookSecurityPolicy(),
            adapter,
            outbound,
            provider.GetRequiredService<ITelegramOperatorInboxService>(),
            new EquipmentDiagnosticTelegramOperationalCounters());

        var ignored = await handler.HandleTrustedAsync(GroupWebhookUpdate("ordinary group chatter", userId: 777));
        var command = await handler.HandleTrustedAsync(GroupWebhookUpdate("/operator_chat_id", userId: 777));

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Ignored, ignored.Status);
        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, command.Status);
        Assert.Equal(0, adapter.CallCount);
        Assert.Contains("TELEGRAM_OPERATOR_CHAT_ID=-100500", outbound.Messages.Single().Text, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateProvider(FakeOutbound outbound)
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton<IEquipmentDiagnosticTelegramOutboundClient>(outbound);
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            MaxMessageLength = 900,
            OperatorInbox = new TelegramOperatorInboxOptions
            {
                Enabled = true,
                ChatId = -100500
            }
        });
        return services.BuildServiceProvider();
    }

    private static async Task CreateUserAsync(
        ServiceProvider provider,
        long chatId,
        long userId,
        TelegramUserRole role)
    {
        var store = provider.GetRequiredService<ITelegramUserStore>();
        await store.GetOrCreateConsumerAsync(new EquipmentDiagnosticTelegramUpdate(1, chatId, "owner", "/start", UserId: userId));
        await store.SetRoleAsync(chatId, role);
    }

    private static async Task<TelegramUserAccessResult> ResolveAccessAsync(
        ServiceProvider provider,
        EquipmentDiagnosticTelegramUpdate update)
    {
        var access = await provider.GetRequiredService<ITelegramUserAccessService>().ResolveAccessAsync(update);
        Assert.True(access.IsAllowed);
        return access;
    }

    private static EquipmentDiagnosticTelegramUpdate UserUpdate(string text) =>
        new(
            UpdateId: 10,
            ChatId: 20,
            Username: "customer",
            Text: text,
            MessageId: 55,
            UserId: 200,
            FirstName: "Иван",
            ChatType: "private");

    private static EquipmentDiagnosticTelegramUpdate DocumentUpdate(string fileId) =>
        UserUpdate("Документ во вложении") with
        {
            Text = "Документ во вложении",
            DocumentFileId = fileId,
            DocumentFileName = "photo.pdf"
        };

    private static EquipmentDiagnosticTelegramUpdate VideoNoteUpdate() =>
        UserUpdate("video note placeholder") with
        {
            Text = null,
            HasVideoNote = true
        };

    private static EquipmentDiagnosticTelegramUpdate GroupUpdate(
        string text,
        long userId,
        long? messageId = null,
        long? replyToMessageId = null) =>
        new(
            UpdateId: 20,
            ChatId: -100500,
            Username: "owner",
            Text: text,
            MessageId: messageId,
            UserId: userId,
            FirstName: "Owner",
            ChatType: "supergroup",
            ReplyToMessageId: replyToMessageId);

    private static EquipmentDiagnosticTelegramUpdate GroupMediaReply(
        TelegramOperatorInboxMessageKind kind,
        long messageId,
        long replyToMessageId)
    {
        var update = GroupUpdate(
            text: "media caption",
            userId: 777,
            messageId: messageId,
            replyToMessageId: replyToMessageId) with
        {
            Text = kind is TelegramOperatorInboxMessageKind.Document or
                TelegramOperatorInboxMessageKind.Photo or
                TelegramOperatorInboxMessageKind.Video or
                TelegramOperatorInboxMessageKind.VideoNote or
                TelegramOperatorInboxMessageKind.Voice or
                TelegramOperatorInboxMessageKind.Audio or
                TelegramOperatorInboxMessageKind.Animation
                    ? null
                    : "media caption"
        };

        return kind switch
        {
            TelegramOperatorInboxMessageKind.Document => update with { DocumentFileId = "operator-document-file-id" },
            TelegramOperatorInboxMessageKind.Photo => update with { HasPhoto = true },
            TelegramOperatorInboxMessageKind.Video => update with { HasVideo = true },
            TelegramOperatorInboxMessageKind.VideoNote => update with { HasVideoNote = true },
            TelegramOperatorInboxMessageKind.Voice => update with { HasVoice = true },
            TelegramOperatorInboxMessageKind.Audio => update with { HasAudio = true },
            TelegramOperatorInboxMessageKind.Contact => update with { ContactPhoneNumber = "+998901234567", ContactUserId = 777 },
            TelegramOperatorInboxMessageKind.Location => update with { Text = null, HasLocation = true },
            TelegramOperatorInboxMessageKind.Animation => update with { HasAnimation = true },
            _ => update
        };
    }

    private static TelegramWebhookUpdateDto GroupWebhookUpdate(
        string text,
        long userId) =>
        new(
            30,
            new TelegramWebhookMessageDto(
                60,
                text,
                new TelegramWebhookChatDto(-100500, null, "supergroup"),
                new TelegramWebhookUserDto(userId, "owner", "Owner"),
                1_700_000_000));

    private sealed class FakeAdapter : IEquipmentDiagnosticTelegramAdapter
    {
        public int CallCount { get; private set; }

        public Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
            EquipmentDiagnosticTelegramUpdate update,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new EquipmentDiagnosticTelegramResponse(
                update.ChatId,
                "adapter response",
                EquipmentDiagnosticTelegramResponseKind.Reply,
                ParseMode: null,
                DisableWebPagePreview: true,
                Warnings: []));
        }
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        private long _nextMessageId = 100;

        public List<(long ChatId, string Text)> Messages { get; } = [];
        public List<(long ChatId, long FromChatId, long MessageId)> CopyMessages { get; } = [];
        public bool FailCopiesToUser { get; init; }

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((chatId, text));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent.", ++_nextMessageId));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Synced."));

        public Task<EquipmentDiagnosticTelegramOutboundResult> CopyMessageAsync(
            long chatId,
            long fromChatId,
            long messageId,
            CancellationToken cancellationToken = default)
        {
            CopyMessages.Add((chatId, fromChatId, messageId));
            if (FailCopiesToUser && chatId == 20)
            {
                return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(false, "Copy failed."));
            }

            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Copied.", ++_nextMessageId));
        }
    }
}
