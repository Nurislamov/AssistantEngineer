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
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Copied.", ++_nextMessageId));
        }
    }
}
