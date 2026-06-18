using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramServiceRequestTests
{
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 6, 17, 17, 45, 0, TimeSpan.Zero);
    private const string FullPhone = "+998901234567";

    [Theory]
    [InlineData("/request")]
    [InlineData("🛠 Нужен мастер")]
    public async Task RequestAliasesCreateServiceRequestFromLatestDiagnosticCase(string text)
    {
        var harness = await CreateHarnessAsync(phoneSaved: true, diagnosticCases: [("H5", "Gree")]);

        var response = await harness.Adapter.HandleAsync(Update(text));
        var requests = await harness.RequestStore.GetLatestForTelegramUserAsync(harness.User.Id, 5);

        var request = Assert.Single(requests);
        Assert.Equal("H5", request.Code);
        Assert.Equal("Gree", request.Manufacturer);
        Assert.Equal(TelegramServiceRequestStatus.New, request.Status);
        Assert.Contains("Заявка создана", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestWithoutDiagnosticCaseIsRejected()
    {
        var harness = await CreateHarnessAsync(phoneSaved: true);

        var response = await harness.Adapter.HandleAsync(Update("/request"));

        Assert.Contains("Сначала отправьте код ошибки", response.Text, StringComparison.Ordinal);
        Assert.Empty(await harness.RequestStore.GetLatestForTelegramUserAsync(harness.User.Id, 5));
    }

    [Fact]
    public async Task RequestWithoutPhoneIsRejectedAndShowsPhoneKeyboard()
    {
        var harness = await CreateHarnessAsync(phoneSaved: false, diagnosticCases: [("H5", "Gree")]);

        var response = await harness.Adapter.HandleAsync(Update("/request"));
        var buttons = ButtonTexts(response);

        Assert.Contains("сначала укажите номер телефона", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(TelegramDiagnosticConversationService.SharePhoneButton, buttons);
        Assert.Contains(TelegramDiagnosticConversationService.ManualPhoneButton, buttons);
        Assert.Contains(TelegramDiagnosticConversationService.NewCodeButton, buttons);
        Assert.Empty(await harness.RequestStore.GetLatestForTelegramUserAsync(harness.User.Id, 5));
    }

    [Fact]
    public async Task DuplicateActiveRequestIsNotCreated()
    {
        var harness = await CreateHarnessAsync(phoneSaved: true, diagnosticCases: [("H5", "Gree")]);

        await harness.Adapter.HandleAsync(Update("/request"));
        var response = await harness.Adapter.HandleAsync(Update("/request"));

        Assert.Single(await harness.RequestStore.GetLatestForTelegramUserAsync(harness.User.Id, 5));
        Assert.Contains("уже есть заявка", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Статус: новая", response.Text, StringComparison.Ordinal);
        Assert.Contains("сегодня 22:45", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestsShowsLatestFiveOwnRequestsInRussianAndLocalTime()
    {
        var harness = await CreateHarnessAsync(phoneSaved: true);
        for (var index = 1; index <= 6; index++)
        {
            var diagnosticCase = await CreateDiagnosticCaseAsync(
                harness.HistoryStore,
                harness.User,
                $"H{index}",
                "Gree",
                FixedNowUtc.AddMinutes(-index));
            await harness.RequestStore.CreateIfNoActiveAsync(
                CreateRequest(harness.User, diagnosticCase, FixedNowUtc.AddMinutes(-index)));
        }

        var other = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start", chatId: 99));
        var otherCase = await CreateDiagnosticCaseAsync(harness.HistoryStore, other, "E6", "Daikin", FixedNowUtc);
        await harness.RequestStore.CreateIfNoActiveAsync(CreateRequest(other, otherCase, FixedNowUtc));

        var response = await harness.Adapter.HandleAsync(Update("/requests"));

        Assert.Contains("Мои заявки", response.Text, StringComparison.Ordinal);
        Assert.Contains("1. Gree H1 — новая — сегодня 22:44", response.Text, StringComparison.Ordinal);
        Assert.Contains("5. Gree H5", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree H6", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Daikin E6", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestsEmptyStateIsRussian()
    {
        var harness = await CreateHarnessAsync(phoneSaved: true);

        var response = await harness.Adapter.HandleAsync(Update("/requests"));

        Assert.Contains("У вас пока нет сервисных заявок", response.Text, StringComparison.Ordinal);
        Assert.Contains("Нужен мастер", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void MainKeyboardIncludesServiceRequestAndRequestsButtons()
    {
        var access = new TelegramUserAccessResult(true, User(hasPhone: true), TelegramUserRole.Consumer);

        var buttons = TelegramDiagnosticConversationService.MainKeyboard(access)
            .Keyboard!
            .SelectMany(row => row)
            .Select(button => button.Text)
            .ToArray();

        Assert.Contains(TelegramDiagnosticConversationService.ServiceRequestButton, buttons);
        Assert.Contains(TelegramDiagnosticConversationService.RequestsButton, buttons);
    }

    [Fact]
    public async Task ConfiguredGroupReceivesSanitizedNotification()
    {
        var harness = await CreateHarnessAsync(
            phoneSaved: true,
            diagnosticCases: [("H5", "Gree")],
            notificationChatId: -1001234567890);

        var response = await harness.Adapter.HandleAsync(Update("/request"));

        var notification = Assert.Single(harness.Outbound.Messages);
        Assert.Equal(-1001234567890, notification.ChatId);
        Assert.Contains("Сервисная заявка #1", notification.Text, StringComparison.Ordinal);
        Assert.Contains("Gree H5", notification.Text, StringComparison.Ordinal);
        Assert.Contains("Статус: новая", notification.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, notification.Text, StringComparison.Ordinal);
        var inlineButtons = notification.ReplyMarkup?.InlineKeyboard?
            .SelectMany(row => row)
            .ToArray() ?? [];
        Assert.Equal(
            ["Взять в работу", "Назначить", "Контакт", "Статус", "Отменить"],
            inlineButtons.Select(button => button.Text).ToArray());
        Assert.All(inlineButtons, button =>
        {
            Assert.StartsWith("sr:", button.CallbackData, StringComparison.Ordinal);
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(button.CallbackData) <= 64);
            Assert.DoesNotContain(FullPhone, button.CallbackData, StringComparison.Ordinal);
        });
        var stored = Assert.Single(await harness.RequestStore.GetLatestForTelegramUserAsync(harness.User.Id, 5));
        Assert.Equal(-1001234567890, stored.NotificationChatId);
        Assert.Equal(700, stored.NotificationMessageId);
        Assert.NotNull(stored.NotificationSentAt);
        Assert.Contains("Заявка создана", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestCreationSucceedsWhenNotificationFails()
    {
        var harness = await CreateHarnessAsync(
            phoneSaved: true,
            diagnosticCases: [("H5", "Gree")],
            notificationChatId: -1001234567890,
            outboundSucceeds: false);

        var response = await harness.Adapter.HandleAsync(Update("/request"));

        Assert.Contains("Заявка создана", response.Text, StringComparison.Ordinal);
        Assert.Single(await harness.RequestStore.GetLatestForTelegramUserAsync(harness.User.Id, 5));
    }

    [Fact]
    public async Task RequestCreationSucceedsWhenNotificationChatIsNotConfigured()
    {
        var harness = await CreateHarnessAsync(phoneSaved: true, diagnosticCases: [("H5", "Gree")]);

        var response = await harness.Adapter.HandleAsync(Update("/request"));

        Assert.Contains("Заявка создана", response.Text, StringComparison.Ordinal);
        Assert.Empty(harness.Outbound.Messages);
        Assert.Single(await harness.RequestStore.GetLatestForTelegramUserAsync(harness.User.Id, 5));
    }

    [Theory]
    [InlineData(TelegramServiceRequestStatus.New, "новая")]
    [InlineData(TelegramServiceRequestStatus.InProgress, "в работе")]
    [InlineData(TelegramServiceRequestStatus.Resolved, "закрыта")]
    [InlineData(TelegramServiceRequestStatus.Cancelled, "отменена")]
    public void StatusLabelsAreRussian(TelegramServiceRequestStatus status, string expected)
    {
        Assert.Equal(expected, TelegramServiceRequestService.StatusLabel(status));
    }

    private static async Task<Harness> CreateHarnessAsync(
        bool phoneSaved,
        IReadOnlyList<(string Code, string Manufacturer)>? diagnosticCases = null,
        long? notificationChatId = null,
        bool outboundSucceeds = true)
    {
        var options = new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            MaxMessageLength = 1000,
            DisplayTimeZone = "Asia/Tashkent",
            ServiceRequests = new TelegramServiceRequestOptions
            {
                NotificationChatId = notificationChatId
            }
        };
        var userStore = new InMemoryTelegramUserStore();
        var historyStore = new InMemoryTelegramDiagnosticCaseStore();
        var requestStore = new InMemoryTelegramServiceRequestStore();
        var outbound = new FakeOutbound(outboundSucceeds);
        var user = await userStore.GetOrCreateConsumerAsync(Update("/start"));
        if (phoneSaved)
        {
            await userStore.SavePhoneAsync(
                user.TelegramChatId,
                FullPhone,
                verified: false,
                TelegramUserPhoneNumberSource.Manual,
                FixedNowUtc);
            user = (await userStore.GetByChatIdAsync(user.TelegramChatId))!;
        }

        foreach (var diagnosticCase in diagnosticCases ?? [])
        {
            await CreateDiagnosticCaseAsync(
                historyStore,
                user,
                diagnosticCase.Code,
                diagnosticCase.Manufacturer,
                FixedNowUtc);
        }

        var timeFormatter = new TelegramDisplayTimeFormatter(options, new FixedTimeProvider());
        var service = new TelegramServiceRequestService(
            requestStore,
            historyStore,
            outbound,
            options,
            timeFormatter,
            new TelegramServiceRequestCardRenderer(userStore, timeFormatter));
        var parser = new EquipmentDiagnosticTelegramMessageParser();
        var adapter = new EquipmentDiagnosticTelegramAdapter(
            new UnusedFacade(),
            parser,
            new EquipmentDiagnosticTelegramResponseFormatter(),
            options,
            new TelegramUserAccessService(userStore, options),
            userStore,
            serviceRequestService: service);
        return new Harness(adapter, userStore, historyStore, requestStore, outbound, user);
    }

    private static Task<TelegramDiagnosticCaseSnapshot> CreateDiagnosticCaseAsync(
        InMemoryTelegramDiagnosticCaseStore store,
        TelegramUserSnapshot user,
        string code,
        string manufacturer,
        DateTimeOffset createdAt) =>
        store.CreateAsync(new TelegramDiagnosticCaseCreate(
            user.Id,
            null,
            TelegramDiagnosticCaseStatus.Completed,
            user.Role,
            TelegramDiagnosticCaseResponseMode.Consumer,
            code,
            manufacturer,
            null,
            null,
            null,
            null,
            1,
            user.HasPhoneNumber,
            user.PhoneNumberSource,
            createdAt));

    private static TelegramServiceRequestCreate CreateRequest(
        TelegramUserSnapshot user,
        TelegramDiagnosticCaseSnapshot diagnosticCase,
        DateTimeOffset createdAt) =>
        new(
            user.Id,
            diagnosticCase.Id,
            diagnosticCase.Code,
            diagnosticCase.Manufacturer,
            diagnosticCase.EquipmentType,
            diagnosticCase.DisplayContext,
            user.HasPhoneNumber,
            user.PhoneNumberSource,
            user.Role,
            createdAt);

    private static EquipmentDiagnosticTelegramUpdate Update(string text, long chatId = 7) =>
        new(
            UpdateId: 1,
            ChatId: chatId,
            Username: "operator",
            Text: text,
            ReceivedAt: FixedNowUtc,
            UserId: chatId + 100);

    private static TelegramUserSnapshot User(bool hasPhone) =>
        new(
            1,
            7,
            107,
            "operator",
            null,
            null,
            TelegramUserRole.Consumer,
            true,
            false,
            false,
            hasPhone,
            hasPhone ? TelegramUserPhoneNumberSource.Manual : null,
            FixedNowUtc,
            FixedNowUtc,
            null);

    private static IReadOnlyList<string> ButtonTexts(EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages
            .SelectMany(message => message.ReplyMarkup?.Keyboard ?? [])
            .SelectMany(row => row)
            .Select(button => button.Text)
            .ToArray();

    private sealed record Harness(
        EquipmentDiagnosticTelegramAdapter Adapter,
        InMemoryTelegramUserStore UserStore,
        InMemoryTelegramDiagnosticCaseStore HistoryStore,
        InMemoryTelegramServiceRequestStore RequestStore,
        FakeOutbound Outbound,
        TelegramUserSnapshot User);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => FixedNowUtc;
    }

    private sealed class FakeOutbound(bool succeeds) : IEquipmentDiagnosticTelegramOutboundClient
    {
        public List<(long ChatId, string Text, EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup)> Messages { get; } = [];

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((chatId, text, replyMarkup));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
                succeeds,
                succeeds ? "Sent." : "Failed.",
                succeeds ? 700 : null));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Synced."));
    }

    private sealed class UnusedFacade : IEquipmentDiagnosticBotFacade
    {
        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
