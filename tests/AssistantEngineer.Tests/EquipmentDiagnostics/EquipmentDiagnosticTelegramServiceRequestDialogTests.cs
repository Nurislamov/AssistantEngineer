using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramServiceRequestDialogTests
{
    [Fact]
    public void RequestCardContainsReplyAndDialogButtonsWithoutRemovingExistingActions()
    {
        var request = Request(12, 1);
        var buttons = TelegramServiceRequestCardRenderer.Keyboard(request).InlineKeyboard!
            .SelectMany(row => row)
            .ToArray();

        Assert.Contains(buttons, item => item.Text == "💬 Ответить" && item.CallbackData == "sr:reply:12");
        Assert.Contains(buttons, item => item.Text == "📜 Диалог" && item.CallbackData == "sr:thread:12");
        Assert.Contains(buttons, item => item.CallbackData == "sr:t:12");
        Assert.All(buttons, item => Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(item.CallbackData), 1, 64));
    }

    [Theory]
    [InlineData(TelegramUserRole.Owner)]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Engineer)]
    public async Task AuthorizedOperatorCanStartAndCompleteTextReply(TelegramUserRole role)
    {
        var harness = await CreateHarnessAsync(role);

        var start = await harness.Service.HandleCallbackAsync(new(
            10, ServiceChatId, "operator", null, 5, UserId: OperatorAccountId,
            ChatType: "supergroup", CallbackData: $"sr:reply:{harness.Request.Id}"));
        var pending = await harness.DialogStore.GetPendingAsync(harness.Operator.Id);

        Assert.True(start.SuppressOutbound);
        Assert.NotNull(pending);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == OperatorChatId && item.Text.Contains($"#{harness.Request.Id}", StringComparison.Ordinal));

        var sent = await harness.Service.TryHandlePrivateMessageAsync(
            new(11, OperatorChatId, "operator", "Проверьте фильтр.", 6, UserId: OperatorAccountId, ChatType: "private"),
            Access(harness.Operator),
            default);

        Assert.Contains("отправлен", sent!.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await harness.DialogStore.GetPendingAsync(harness.Operator.Id));
        var history = await harness.DialogStore.GetLatestMessagesAsync(harness.Request.Id, 10);
        Assert.Contains(history, item =>
            item.Direction == TelegramServiceRequestMessageDirection.OperatorToUser &&
            item.Text == "Проверьте фильтр.");
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == RequesterChatId && item.Text.Contains("Проверьте фильтр.", StringComparison.Ordinal));
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == ServiceChatId && item.Text.Contains("Ответ отправлен пользователю", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConsumerCannotStartOperatorReply()
    {
        var harness = await CreateHarnessAsync(TelegramUserRole.Consumer);

        var result = await harness.Service.HandleCallbackAsync(new(
            10, ServiceChatId, "operator", null, 5, UserId: OperatorAccountId,
            ChatType: "supergroup", CallbackData: $"sr:reply:{harness.Request.Id}"));

        Assert.True(result.SuppressOutbound);
        Assert.Equal("Нет доступа", result.CallbackAnswerText);
        Assert.Null(await harness.DialogStore.GetPendingAsync(harness.Operator.Id));
    }

    [Fact]
    public async Task CancelClearsPendingReply()
    {
        var harness = await CreateHarnessAsync(TelegramUserRole.Engineer);
        await harness.Service.HandleCallbackAsync(new(
            10, ServiceChatId, "operator", null, 5, UserId: OperatorAccountId,
            ChatType: "supergroup", CallbackData: $"sr:reply:{harness.Request.Id}"));

        var result = await harness.Service.TryHandlePrivateMessageAsync(
            new(11, OperatorChatId, "operator", "/cancel", UserId: OperatorAccountId, ChatType: "private"),
            Access(harness.Operator));

        Assert.Contains("отменён", result!.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await harness.DialogStore.GetPendingAsync(harness.Operator.Id));
    }

    [Fact]
    public async Task UserTextAfterOperatorReplyIsSavedAndSentToGroup()
    {
        var harness = await CreateHarnessAsync(TelegramUserRole.Engineer);
        await harness.DialogStore.AddMessageAsync(new(
            harness.Request.Id,
            TelegramServiceRequestMessageDirection.OperatorToUser,
            harness.Operator.Id,
            harness.Operator.Role,
            "Уточните модель.",
            RequesterChatId,
            100,
            DateTimeOffset.UtcNow));

        var result = await harness.Service.TryHandlePrivateMessageAsync(
            new(12, RequesterChatId, "requester", "Модель GWH12.", 101, UserId: RequesterAccountId, ChatType: "private"),
            Access(harness.Requester));

        Assert.Contains($"#{harness.Request.Id}", result!.Text, StringComparison.Ordinal);
        var history = await harness.DialogStore.GetLatestMessagesAsync(harness.Request.Id, 10);
        Assert.Contains(history, item =>
            item.Direction == TelegramServiceRequestMessageDirection.UserToOperator &&
            item.Text == "Модель GWH12.");
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == ServiceChatId &&
            item.Text.Contains("Ответ пользователя", StringComparison.Ordinal) &&
            item.Markup?.InlineKeyboard?.SelectMany(row => row).Any(button => button.Text == "💬 Ответить") == true);
    }

    [Theory]
    [InlineData("/start")]
    [InlineData("/help")]
    [InlineData("/history")]
    [InlineData("/last")]
    public async Task UserCommandsAreNotSwallowedAsDialogReplies(string command)
    {
        var harness = await CreateHarnessAsync(TelegramUserRole.Engineer);
        await harness.DialogStore.AddMessageAsync(new(
            harness.Request.Id, TelegramServiceRequestMessageDirection.OperatorToUser,
            harness.Operator.Id, harness.Operator.Role, "Ответ", null, null, DateTimeOffset.UtcNow));

        var result = await harness.Service.TryHandlePrivateMessageAsync(
            new(12, RequesterChatId, "requester", command, UserId: RequesterAccountId, ChatType: "private"),
            Access(harness.Requester));

        Assert.Null(result);
    }

    [Fact]
    public async Task MultipleActiveRequestsRequireExplicitSelection()
    {
        var harness = await CreateHarnessAsync(TelegramUserRole.Engineer);
        var second = (await harness.RequestStore.CreateIfNoActiveAsync(new(
            harness.Requester.Id, 88, "E6", "Gree", null, null, true, null,
            TelegramUserRole.Consumer, DateTimeOffset.UtcNow))).Request;
        // The in-memory store prevents two active requests for one diagnostic case, not for different cases.
        await harness.DialogStore.AddMessageAsync(new(
            harness.Request.Id, TelegramServiceRequestMessageDirection.OperatorToUser,
            harness.Operator.Id, harness.Operator.Role, "Первый", null, null, DateTimeOffset.UtcNow));
        await harness.DialogStore.AddMessageAsync(new(
            second.Id, TelegramServiceRequestMessageDirection.OperatorToUser,
            harness.Operator.Id, harness.Operator.Role, "Второй", null, null, DateTimeOffset.UtcNow));

        var result = await harness.Service.TryHandlePrivateMessageAsync(
            new(12, RequesterChatId, "requester", "Уточнение", UserId: RequesterAccountId, ChatType: "private"),
            Access(harness.Requester));

        Assert.Equal("К какой заявке относится сообщение?", result!.Text);
        Assert.Equal(2, result.ReplyMarkup!.InlineKeyboard!.Count);
    }

    [Fact]
    public async Task DialogShowsRecentMessagesAndNeverIncludesPhone()
    {
        var harness = await CreateHarnessAsync(TelegramUserRole.Admin);
        await harness.DialogStore.AddMessageAsync(new(
            harness.Request.Id, TelegramServiceRequestMessageDirection.UserToOperator,
            harness.Requester.Id, TelegramUserRole.Consumer, "Описание ошибки", null, null, DateTimeOffset.UtcNow));

        var result = await harness.Service.HandleCallbackAsync(new(
            13, ServiceChatId, "operator", null, 8, UserId: OperatorAccountId,
            ChatType: "supergroup", CallbackData: $"sr:thread:{harness.Request.Id}"));

        Assert.Contains("Описание ошибки", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("телефон", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlockedRequesterCannotReceiveOperatorReply()
    {
        var harness = await CreateHarnessAsync(TelegramUserRole.Engineer);
        await harness.UserStore.SetBlockedAsync(RequesterChatId, true);

        var result = await harness.Service.HandleCallbackAsync(new(
            10, ServiceChatId, "operator", null, 5, UserId: OperatorAccountId,
            ChatType: "supergroup", CallbackData: $"sr:reply:{harness.Request.Id}"));

        Assert.Contains("заблокирован", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await harness.DialogStore.GetPendingAsync(harness.Operator.Id));
    }

    private static async Task<Harness> CreateHarnessAsync(TelegramUserRole operatorRole)
    {
        var users = new InMemoryTelegramUserStore();
        var requester = await users.GetOrCreateConsumerAsync(new(
            1, RequesterChatId, "requester", "/start", UserId: RequesterAccountId, ChatType: "private"));
        var operatorUser = await users.GetOrCreateConsumerAsync(new(
            2, OperatorChatId, "operator", "/start", UserId: OperatorAccountId, ChatType: "private"));
        await users.AllowAsync(OperatorChatId, operatorRole);
        operatorUser = (await users.GetByChatIdAsync(OperatorChatId))!;

        var requests = new InMemoryTelegramServiceRequestStore();
        var request = (await requests.CreateIfNoActiveAsync(new(
            requester.Id, 77, "PF", "Gree", null, null, true, null,
            TelegramUserRole.Consumer, DateTimeOffset.UtcNow))).Request;
        var dialog = new InMemoryTelegramServiceRequestDialogStore();
        var outbound = new FakeOutbound();
        var options = new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            ServiceRequests = new TelegramServiceRequestOptions { NotificationChatId = ServiceChatId }
        };
        var events = new TelegramServiceRequestEventService(
            new InMemoryTelegramServiceRequestEventStore(),
            users,
            new TelegramDisplayTimeFormatter(options));
        var service = new TelegramServiceRequestDialogService(
            dialog, requests, users, outbound, options, new TelegramDisplayTimeFormatter(options), events);
        return new(service, dialog, requests, users, outbound, request, requester, operatorUser);
    }

    private static TelegramUserAccessResult Access(TelegramUserSnapshot user) =>
        new(true, user, user.Role);

    private static TelegramServiceRequestSnapshot Request(long id, long userId) =>
        new(id, userId, 1, TelegramServiceRequestSource.Telegram, TelegramServiceRequestStatus.New,
            "PF", "Gree", null, null, true, null, null, TelegramUserRole.Consumer,
            null, null, null, null, null, DateTimeOffset.UtcNow, null, null,
            null, null, null, null);

    private const long ServiceChatId = -100500;
    private const long RequesterChatId = 100;
    private const long RequesterAccountId = 1000;
    private const long OperatorChatId = 200;
    private const long OperatorAccountId = 2000;

    private sealed record SentMessage(long ChatId, string Text, EquipmentDiagnosticTelegramReplyMarkup? Markup);

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public List<SentMessage> Messages { get; } = [];

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(new(chatId, text, replyMarkup));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "ok", Messages.Count));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "ok"));
    }

    private sealed record Harness(
        TelegramServiceRequestDialogService Service,
        InMemoryTelegramServiceRequestDialogStore DialogStore,
        InMemoryTelegramServiceRequestStore RequestStore,
        InMemoryTelegramUserStore UserStore,
        FakeOutbound Outbound,
        TelegramServiceRequestSnapshot Request,
        TelegramUserSnapshot Requester,
        TelegramUserSnapshot Operator);
}
