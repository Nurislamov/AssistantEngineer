using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramServiceRequestQueueTests
{
    private const long ServiceGroupId = -1001234567890;
    private const string FullPhone = "+998901234567";
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 6, 18, 12, 20, 0, TimeSpan.Zero);

    [Fact]
    public async Task QueueShowsActiveRequestsAndRussianEmptyState()
    {
        var harness = await CreateHarnessAsync();
        var first = await harness.CreateRequestAsync("H5");
        var second = await harness.CreateRequestAsync("C5");
        await harness.RequestStore.UpdateAsync(Update(second.Id, TelegramServiceRequestStatus.InProgress, harness.Engineer.Id));

        var response = await harness.HandleAsync("/queue", harness.Engineer);

        Assert.Contains($"#{first.Id} — Gree H5 — новая", response, StringComparison.Ordinal);
        Assert.Contains($"#{second.Id} — Gree C5 — в работе — @engineer", response, StringComparison.Ordinal);
        Assert.Contains("Телефон: сохранён", response, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, response, StringComparison.Ordinal);

        await harness.RequestStore.UpdateAsync(Update(first.Id, TelegramServiceRequestStatus.Resolved, harness.Engineer.Id));
        await harness.RequestStore.UpdateAsync(Update(second.Id, TelegramServiceRequestStatus.Cancelled, harness.Engineer.Id));
        Assert.Equal("Активных сервисных заявок нет.", await harness.HandleAsync("/queue", harness.Engineer));
    }

    [Fact]
    public async Task QueueResponseIncludesInlineActionsForActiveRequests()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCommandResultAsync("/queue", harness.Engineer);
        var buttons = InlineButtons(result);

        Assert.Contains(buttons, button => button.Text == $"#{request.Id} Статус" && button.CallbackData == $"sr:s:{request.Id}");
        Assert.Contains(buttons, button => button.Text == $"#{request.Id} Взять" && button.CallbackData == $"sr:t:{request.Id}");
        Assert.Contains(buttons, button => button.Text == $"#{request.Id} Назначить" && button.CallbackData == $"sr:a:{request.Id}");
    }

    [Fact]
    public async Task CallbackTakeAssignsEngineerAndKeepsPhonePrivate()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCallbackAsync($"sr:t:{request.Id}", harness.Engineer);
        var updated = await harness.RequestStore.GetByIdAsync(request.Id);

        Assert.Contains("взята в работу", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(harness.Engineer.Id, updated?.AssignedTelegramUserId);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Customer.TelegramChatId &&
            item.Text.Contains("Ваша заявка", StringComparison.Ordinal));
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Engineer.TelegramChatId &&
            item.Text.Contains(FullPhone, StringComparison.Ordinal));
        Assert.DoesNotContain(FullPhone, result.Text, StringComparison.Ordinal);
        var edit = Assert.Single(harness.Outbound.Edits);
        Assert.Contains("Статус: в работе", edit.Text, StringComparison.Ordinal);
        Assert.Contains(InlineButtons(edit.ReplyMarkup), button => button.Text == "Закрыть");
        Assert.DoesNotContain(harness.Outbound.Messages, item => item.ChatId == ServiceGroupId);
    }

    [Fact]
    public async Task CallbackAssignMenuEditsCardAndBackRestoresCurrentCard()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var menu = await harness.HandleCallbackAsync($"sr:a:{request.Id}", harness.Admin);

        Assert.Contains(InlineButtons(menu), button => button.Text == "Назад" && button.CallbackData == $"sr:b:{request.Id}");
        Assert.Contains("Выберите инженера", Assert.Single(harness.Outbound.Edits).Text, StringComparison.Ordinal);

        harness.Outbound.Edits.Clear();
        var back = await harness.HandleCallbackAsync($"sr:b:{request.Id}", harness.Admin);

        Assert.Contains("Сервисная заявка", back.Text, StringComparison.Ordinal);
        var restored = Assert.Single(harness.Outbound.Edits);
        Assert.Contains("Статус: новая", restored.Text, StringComparison.Ordinal);
        Assert.Contains(InlineButtons(restored.ReplyMarkup), button => button.Text == "Взять в работу");
    }

    [Fact]
    public async Task CallbackAssignMenuAndSelectionEnforceAdminAndAssignPrivately()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var denied = await harness.HandleCallbackAsync($"sr:a:{request.Id}", harness.Engineer);
        var consumerDenied = await harness.HandleCallbackAsync($"sr:a:{request.Id}", harness.Consumer);
        var menu = await harness.HandleCallbackAsync($"sr:a:{request.Id}", harness.Admin);
        var engineerButton = Assert.Single(
            InlineButtons(menu),
            button => button.CallbackData == $"sr:as:{request.Id}:{harness.Engineer.Id}");
        var assigned = await harness.HandleCallbackAsync(engineerButton.CallbackData, harness.Admin);

        Assert.Contains("только Owner или Admin", denied.Text, StringComparison.Ordinal);
        Assert.Contains("только Owner или Admin", consumerDenied.Text, StringComparison.Ordinal);
        Assert.Contains("Выберите инженера", menu.Text, StringComparison.Ordinal);
        Assert.Equal("@engineer", engineerButton.Text);
        Assert.Contains("@engineer", assigned.Text, StringComparison.Ordinal);
        Assert.Equal(harness.Engineer.Id, (await harness.RequestStore.GetByIdAsync(request.Id))?.AssignedTelegramUserId);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Engineer.TelegramChatId &&
            item.Text.Contains(FullPhone, StringComparison.Ordinal));
        Assert.DoesNotContain(FullPhone, assigned.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CallbackContactIsPrivateAndRestricted()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.HandleCallbackAsync($"sr:t:{request.Id}", harness.Engineer);
        harness.Outbound.Messages.Clear();
        harness.Outbound.Edits.Clear();

        var denied = await harness.HandleCallbackAsync($"sr:c:{request.Id}", harness.OtherEngineer);
        var assigned = await harness.HandleCallbackAsync($"sr:c:{request.Id}", harness.Engineer);
        var admin = await harness.HandleCallbackAsync($"sr:c:{request.Id}", harness.Admin);

        Assert.Contains("только назначенному инженеру", denied.Text, StringComparison.Ordinal);
        Assert.Equal("Контакт отправлен в личный чат.", assigned.Text);
        Assert.Equal("Контакт отправлен в личный чат.", admin.Text);
        Assert.Contains(harness.Outbound.Messages, item => item.ChatId == harness.Engineer.TelegramChatId && item.Text.Contains(FullPhone));
        Assert.Contains(harness.Outbound.Messages, item => item.ChatId == harness.Admin.TelegramChatId && item.Text.Contains(FullPhone));
        Assert.DoesNotContain(FullPhone, assigned.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, admin.Text, StringComparison.Ordinal);
        Assert.Empty(harness.Outbound.Edits);
        Assert.DoesNotContain(harness.Outbound.Messages, item => item.ChatId == ServiceGroupId);
    }

    [Fact]
    public async Task CallbackDoneCancelStatusAndGroupScopeFollowCommandRules()
    {
        var harness = await CreateHarnessAsync();
        var first = await harness.CreateRequestAsync("H5");
        await harness.HandleCallbackAsync($"sr:t:{first.Id}", harness.Engineer);

        var denied = await harness.HandleCallbackAsync($"sr:d:{first.Id}", harness.OtherEngineer);
        var status = await harness.HandleCallbackAsync($"sr:s:{first.Id}", harness.Engineer);
        var resolved = await harness.HandleCallbackAsync($"sr:d:{first.Id}", harness.Engineer);

        Assert.Contains("только назначенный инженер", denied.Text, StringComparison.Ordinal);
        Assert.Contains("Статус: в работе", status.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, status.Text, StringComparison.Ordinal);
        Assert.Contains("закрыта", resolved.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramServiceRequestStatus.Resolved, (await harness.RequestStore.GetByIdAsync(first.Id))?.Status);

        var second = await harness.CreateRequestAsync("C5");
        await harness.HandleCallbackAsync($"sr:t:{second.Id}", harness.Engineer);
        var cancelled = await harness.HandleCallbackAsync($"sr:x:{second.Id}", harness.Engineer);
        var outside = await harness.HandleCallbackAsync($"sr:s:{second.Id}", harness.Engineer, chatId: 42);

        Assert.Contains("отменена", cancelled.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramServiceRequestStatus.Cancelled, (await harness.RequestStore.GetByIdAsync(second.Id))?.Status);
        Assert.Equal("Действие доступно в сервисной группе.", outside.Text);
        Assert.Contains(harness.Outbound.Edits, edit =>
            edit.Text.Contains("Статус: закрыта", StringComparison.Ordinal) &&
            InlineButtons(edit.ReplyMarkup).All(button => button.Text == "Статус"));
        Assert.Contains(harness.Outbound.Edits, edit =>
            edit.Text.Contains("Статус: отменена", StringComparison.Ordinal) &&
            InlineButtons(edit.ReplyMarkup).All(button => button.Text == "Статус"));
    }

    [Fact]
    public async Task CallbackStatusRefreshesCardWithoutGroupMessage()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCallbackAsync($"sr:s:{request.Id}", harness.Engineer);

        Assert.Equal("Статус обновлён", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
        Assert.Single(harness.Outbound.Edits);
        Assert.DoesNotContain(harness.Outbound.Messages, item => item.ChatId == ServiceGroupId);
    }

    [Fact]
    public async Task FailedEditKeepsStatusAndSendsAndStoresFallbackCard()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        harness.Outbound.EditSucceeds = false;

        await harness.HandleCallbackAsync($"sr:t:{request.Id}", harness.Engineer);

        var updated = await harness.RequestStore.GetByIdAsync(request.Id);
        Assert.Equal(TelegramServiceRequestStatus.InProgress, updated?.Status);
        Assert.NotEqual(9000 + request.Id, updated?.NotificationMessageId);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == ServiceGroupId &&
            item.Text.Contains("Статус: в работе", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("take")]
    [InlineData("assign")]
    [InlineData("done")]
    [InlineData("cancel")]
    public async Task StatusChangingCommandsRefreshStoredCard(string action)
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        switch (action)
        {
            case "take":
                await harness.HandleAsync($"/take {request.Id}", harness.Engineer);
                break;
            case "assign":
                await harness.HandleAsync($"/assign {request.Id} @engineer", harness.Admin);
                break;
            case "done":
                await harness.HandleAsync($"/done {request.Id}", harness.Admin);
                break;
            case "cancel":
                await harness.HandleAsync($"/cancel_request {request.Id}", harness.Admin);
                break;
        }

        Assert.Single(harness.Outbound.Edits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bad")]
    [InlineData("sr:t:nope")]
    [InlineData("sr:unknown:1")]
    public async Task InvalidCallbackDataIsHandledSafely(string? data)
    {
        var harness = await CreateHarnessAsync();

        var result = await harness.HandleCallbackAsync(data, harness.Engineer);

        Assert.Equal("Действие недоступно или устарело.", result.Text);
    }

    [Fact]
    public async Task TakeByEngineerAssignsRequestAndSendsCustomerAndContactPrivately()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var response = await harness.HandleAsync($"/take {request.Id}", harness.Engineer);
        var updated = await harness.RequestStore.GetByIdAsync(request.Id);

        Assert.Equal(TelegramServiceRequestStatus.InProgress, updated?.Status);
        Assert.Equal(harness.Engineer.Id, updated?.AssignedTelegramUserId);
        Assert.NotNull(updated?.AssignedAt);
        Assert.Contains("взята в работу", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Customer.TelegramChatId &&
            item.Text.Contains("Ваша заявка", StringComparison.Ordinal));
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Engineer.TelegramChatId &&
            item.Text.Contains(FullPhone, StringComparison.Ordinal));
        Assert.DoesNotContain(FullPhone, response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TakeSupportsBotUsernameSyntax()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var response = await harness.HandleAsync($"/take@EquipmentBot {request.Id}", harness.Engineer);

        Assert.Contains("взята в работу", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TakeByConsumerOrUnknownUserIsDenied()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var consumer = await harness.HandleAsync($"/take {request.Id}", harness.Consumer);
        var unknown = await harness.HandleRawAsync($"/take {request.Id}", senderTelegramUserId: 999999);

        Assert.Equal("Команда недоступна.", consumer);
        Assert.Contains("Сначала откройте бота в личке", unknown, StringComparison.Ordinal);
        Assert.Equal(TelegramServiceRequestStatus.New, (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
    }

    [Fact]
    public async Task QueueCommandsOutsideServiceGroupAreRejected()
    {
        var harness = await CreateHarnessAsync();

        var response = await harness.HandleRawAsync("/queue", harness.Engineer.TelegramUserId!.Value, chatId: 42);

        Assert.Equal("Команда доступна в сервисной группе.", response);
    }

    [Fact]
    public async Task AssignByAdminFindsEngineerByUsernameAndSendsContact()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var response = await harness.HandleAsync($"/assign {request.Id} @ENGINEER", harness.Admin);
        var updated = await harness.RequestStore.GetByIdAsync(request.Id);

        Assert.Equal(harness.Engineer.Id, updated?.AssignedTelegramUserId);
        Assert.Equal(harness.Admin.Id, updated?.AssignedByTelegramUserId);
        Assert.Contains("@engineer", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Engineer.TelegramChatId &&
            item.Text.Contains(FullPhone, StringComparison.Ordinal));
        Assert.DoesNotContain(FullPhone, response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssignByEngineerIsDeniedAndTargetMustHaveServiceRole()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var denied = await harness.HandleAsync($"/assign {request.Id} @admin", harness.Engineer);
        var wrongRole = await harness.HandleAsync($"/assign {request.Id} @consumer", harness.Admin);
        var missing = await harness.HandleAsync($"/assign {request.Id} @missing", harness.Admin);

        Assert.Contains("только Owner или Admin", denied, StringComparison.Ordinal);
        Assert.Contains("не имеет роли Engineer", wrongRole, StringComparison.Ordinal);
        Assert.Contains("Инженер не найден", missing, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssignedEngineerCanResolveAndCancelButOtherEngineerCannot()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        var denied = await harness.HandleAsync($"/done {request.Id}", harness.OtherEngineer);
        var resolved = await harness.HandleAsync($"/done {request.Id}", harness.Engineer);

        Assert.Contains("только назначенный инженер", denied, StringComparison.Ordinal);
        Assert.Contains("закрыта", resolved, StringComparison.Ordinal);
        Assert.Equal(TelegramServiceRequestStatus.Resolved, (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Customer.TelegramChatId &&
            item.Text.Contains("закрыта", StringComparison.Ordinal));

        var second = await harness.CreateRequestAsync("C5");
        await harness.HandleAsync($"/take {second.Id}", harness.Engineer);
        var cancelDenied = await harness.HandleAsync($"/cancel_request {second.Id}", harness.OtherEngineer);
        var cancelled = await harness.HandleAsync($"/cancel_request {second.Id}", harness.Engineer);
        Assert.Contains("только назначенный инженер", cancelDenied, StringComparison.Ordinal);
        Assert.Contains("отменена", cancelled, StringComparison.Ordinal);
        Assert.Equal(TelegramServiceRequestStatus.Cancelled, (await harness.RequestStore.GetByIdAsync(second.Id))?.Status);
        Assert.Contains(harness.Outbound.Messages, item =>
            item.ChatId == harness.Customer.TelegramChatId &&
            item.Text.Contains("отменена", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OwnerCanCloseUnassignedRequest()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var response = await harness.HandleAsync($"/done {request.Id}", harness.Admin);

        Assert.Contains("закрыта", response, StringComparison.Ordinal);
        Assert.Equal(TelegramServiceRequestStatus.Resolved, (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
    }

    [Fact]
    public async Task RequestStatusNeverShowsFullPhone()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        var response = await harness.HandleAsync($"/request_status {request.Id}", harness.Engineer);

        Assert.Contains("Статус: в работе", response, StringComparison.Ordinal);
        Assert.Contains("Инженер: @engineer", response, StringComparison.Ordinal);
        Assert.Contains("Телефон: сохранён", response, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContactIsPrivateAndRestrictedToAssigneeOrAdmin()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);
        harness.Outbound.Messages.Clear();

        var denied = await harness.HandleAsync($"/contact {request.Id}", harness.OtherEngineer);
        var assigned = await harness.HandleAsync($"/contact {request.Id}", harness.Engineer);
        var admin = await harness.HandleAsync($"/contact {request.Id}", harness.Admin);

        Assert.Contains("только назначенному инженеру", denied, StringComparison.Ordinal);
        Assert.Equal("Контакт отправлен в личный чат.", assigned);
        Assert.Equal("Контакт отправлен в личный чат.", admin);
        Assert.Contains(harness.Outbound.Messages, item => item.ChatId == harness.Engineer.TelegramChatId && item.Text.Contains(FullPhone));
        Assert.Contains(harness.Outbound.Messages, item => item.ChatId == harness.Admin.TelegramChatId && item.Text.Contains(FullPhone));
        Assert.DoesNotContain(FullPhone, assigned, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, admin, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContactPrivateFailureReturnsSafeGroupMessage()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);
        harness.Outbound.FailingChatIds.Add(harness.Engineer.TelegramChatId);

        var response = await harness.HandleAsync($"/contact {request.Id}", harness.Engineer);

        Assert.Contains("Откройте личный чат с ботом", response, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, response, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Logger.Messages, item => item.Contains(FullPhone, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CustomerNotificationFailureDoesNotRollbackStatus()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        harness.Outbound.FailingChatIds.Add(harness.Customer.TelegramChatId);

        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        Assert.Equal(TelegramServiceRequestStatus.InProgress, (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
    }

    [Fact]
    public async Task RequestsListReflectsUpdatedRussianStatus()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.HandleAsync($"/done {request.Id}", harness.Admin);
        var service = new TelegramServiceRequestService(
            harness.RequestStore,
            harness.HistoryStore,
            harness.Outbound,
            harness.Options,
            harness.TimeFormatter,
            new TelegramServiceRequestCardRenderer(harness.UserStore, harness.TimeFormatter));

        var text = await service.FormatRequestsAsync(harness.Customer);

        Assert.Contains("Gree H5 — закрыта", text, StringComparison.Ordinal);
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var options = new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            DisplayTimeZone = "Asia/Tashkent",
            ServiceRequests = new TelegramServiceRequestOptions
            {
                NotificationChatId = ServiceGroupId
            }
        };
        var users = new InMemoryTelegramUserStore();
        var customer = await CreateUserAsync(users, 10, 1010, "customer", TelegramUserRole.Consumer, savePhone: true);
        var engineer = await CreateUserAsync(users, 20, 2020, "engineer", TelegramUserRole.Engineer);
        var otherEngineer = await CreateUserAsync(users, 21, 2121, "otherengineer", TelegramUserRole.Engineer);
        var admin = await CreateUserAsync(users, 30, 3030, "admin", TelegramUserRole.Admin);
        var consumer = await CreateUserAsync(users, 40, 4040, "consumer", TelegramUserRole.Consumer);
        var requests = new InMemoryTelegramServiceRequestStore();
        var history = new InMemoryTelegramDiagnosticCaseStore();
        var outbound = new FakeOutbound();
        var formatter = new TelegramDisplayTimeFormatter(options, new FixedTimeProvider());
        var logger = new CapturingLogger<TelegramServiceRequestQueueService>();
        var service = new TelegramServiceRequestQueueService(
            requests,
            users,
            outbound,
            options,
            formatter,
            new TelegramServiceRequestCardRenderer(users, formatter),
            logger);
        return new Harness(options, service, users, requests, history, outbound, formatter, logger, customer, engineer, otherEngineer, admin, consumer);
    }

    private static async Task<TelegramUserSnapshot> CreateUserAsync(
        InMemoryTelegramUserStore store,
        long chatId,
        long telegramUserId,
        string username,
        TelegramUserRole role,
        bool savePhone = false)
    {
        var update = new EquipmentDiagnosticTelegramUpdate(
            1,
            chatId,
            username,
            "/start",
            ReceivedAt: FixedNowUtc,
            UserId: telegramUserId,
            ChatType: "private");
        var user = await store.GetOrCreateConsumerAsync(update);
        await store.SetRoleAsync(chatId, role);
        if (savePhone)
        {
            await store.SavePhoneAsync(chatId, FullPhone, false, TelegramUserPhoneNumberSource.Manual, FixedNowUtc);
        }
        return (await store.GetByChatIdAsync(chatId))!;
    }

    private static TelegramServiceRequestUpdate Update(
        long id,
        TelegramServiceRequestStatus status,
        long? assignee) =>
        new(id, status, assignee, FixedNowUtc, assignee, FixedNowUtc, assignee ?? 1, status is TelegramServiceRequestStatus.Resolved or TelegramServiceRequestStatus.Cancelled ? FixedNowUtc : null);

    private sealed record Harness(
        EquipmentDiagnosticTelegramOptions Options,
        TelegramServiceRequestQueueService Service,
        InMemoryTelegramUserStore UserStore,
        InMemoryTelegramServiceRequestStore RequestStore,
        InMemoryTelegramDiagnosticCaseStore HistoryStore,
        FakeOutbound Outbound,
        TelegramDisplayTimeFormatter TimeFormatter,
        CapturingLogger<TelegramServiceRequestQueueService> Logger,
        TelegramUserSnapshot Customer,
        TelegramUserSnapshot Engineer,
        TelegramUserSnapshot OtherEngineer,
        TelegramUserSnapshot Admin,
        TelegramUserSnapshot Consumer)
    {
        public async Task<TelegramServiceRequestSnapshot> CreateRequestAsync(string code)
        {
            var diagnosticCase = await HistoryStore.CreateAsync(new TelegramDiagnosticCaseCreate(
                Customer.Id,
                null,
                TelegramDiagnosticCaseStatus.Completed,
                Customer.Role,
                TelegramDiagnosticCaseResponseMode.Consumer,
                code,
                "Gree",
                null,
                null,
                null,
                null,
                1,
                true,
                TelegramUserPhoneNumberSource.Manual,
                FixedNowUtc));
            var request = (await RequestStore.CreateIfNoActiveAsync(new TelegramServiceRequestCreate(
                Customer.Id,
                diagnosticCase.Id,
                code,
                "Gree",
                null,
                null,
                true,
                TelegramUserPhoneNumberSource.Manual,
                Customer.Role,
                FixedNowUtc))).Request;
            return (await RequestStore.UpdateNotificationAsync(
                new TelegramServiceRequestNotificationUpdate(
                    request.Id,
                    ServiceGroupId,
                    9000 + request.Id,
                    FixedNowUtc,
                    FixedNowUtc)))!;
        }

        public Task<string> HandleAsync(string text, TelegramUserSnapshot sender) =>
            HandleRawAsync(text, sender.TelegramUserId!.Value);

        public Task<TelegramServiceQueueCommandResult> HandleCommandResultAsync(
            string text,
            TelegramUserSnapshot sender)
        {
            Assert.True(TelegramServiceRequestQueueService.TryParse(text, out var command));
            return Service.HandleAsync(
                new EquipmentDiagnosticTelegramUpdate(
                    1,
                    ServiceGroupId,
                    sender.Username,
                    text,
                    ReceivedAt: FixedNowUtc,
                    UserId: sender.TelegramUserId,
                    ChatType: "supergroup"),
                command);
        }

        public Task<TelegramServiceQueueCommandResult> HandleCallbackAsync(
            string? data,
            TelegramUserSnapshot sender,
            long chatId = ServiceGroupId) =>
            Service.HandleCallbackAsync(
                new EquipmentDiagnosticTelegramUpdate(
                    1,
                    chatId,
                    sender.Username,
                    Text: null,
                    ReceivedAt: FixedNowUtc,
                    UserId: sender.TelegramUserId,
                    ChatType: "supergroup",
                    CallbackQueryId: "callback-id",
                    CallbackData: data));

        public async Task<string> HandleRawAsync(string text, long senderTelegramUserId, long chatId = ServiceGroupId)
        {
            Assert.True(TelegramServiceRequestQueueService.TryParse(text, out var command));
            var result = await Service.HandleAsync(
                new EquipmentDiagnosticTelegramUpdate(
                    1,
                    chatId,
                    "sender",
                    text,
                    ReceivedAt: FixedNowUtc,
                    UserId: senderTelegramUserId,
                    ChatType: "supergroup"),
                command);
            return result.Text;
        }
    }

    private static IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton> InlineButtons(
        TelegramServiceQueueCommandResult result) =>
        result.ReplyMarkup?.InlineKeyboard?
            .SelectMany(row => row)
            .ToArray() ?? [];

    private static IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton> InlineButtons(
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup) =>
        replyMarkup?.InlineKeyboard?
            .SelectMany(row => row)
            .ToArray() ?? [];

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => FixedNowUtc;
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public List<(long ChatId, string Text)> Messages { get; } = [];
        public List<(long ChatId, long MessageId, string Text, EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup)> Edits { get; } = [];
        public HashSet<long> FailingChatIds { get; } = [];
        public bool EditSucceeds { get; set; } = true;
        private long _nextMessageId = 10000;

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((chatId, text));
            var succeeded = !FailingChatIds.Contains(chatId);
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
                succeeded,
                succeeded ? "Sent." : "Failed.",
                succeeded ? Interlocked.Increment(ref _nextMessageId) : null));
        }

        public Task<EquipmentDiagnosticTelegramOutboundResult> EditMessageTextAsync(
            long chatId,
            long messageId,
            string text,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Edits.Add((chatId, messageId, text, replyMarkup));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
                EditSucceeds,
                EditSucceeds ? "Edited." : "Failed.",
                EditSucceeds ? messageId : null));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Synced."));
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
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
