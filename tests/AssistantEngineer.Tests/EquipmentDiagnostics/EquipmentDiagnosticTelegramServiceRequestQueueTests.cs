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
    public async Task QueueDefaultsToActiveAndActiveShowsNewAndInProgressOnly()
    {
        var harness = await CreateHarnessAsync();
        var fresh = await harness.CreateRequestAsync("H5");
        var inProgress = await harness.CreateRequestAsync("C5");
        var resolved = await harness.CreateRequestAsync("E6");
        await harness.RequestStore.UpdateAsync(Update(
            inProgress.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.Engineer.Id));
        await harness.RequestStore.UpdateAsync(Update(
            resolved.Id,
            TelegramServiceRequestStatus.Resolved,
            harness.Engineer.Id));

        var defaultQueue = await harness.HandleAsync("/queue", harness.Engineer);
        var activeQueue = await harness.HandleAsync("/queue active", harness.Engineer);

        Assert.Contains($"#{fresh.Id}", defaultQueue, StringComparison.Ordinal);
        Assert.Contains($"#{inProgress.Id}", defaultQueue, StringComparison.Ordinal);
        Assert.DoesNotContain($"#{resolved.Id}", defaultQueue, StringComparison.Ordinal);
        Assert.Equal(defaultQueue, activeQueue);
        Assert.DoesNotContain(FullPhone, defaultQueue, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueueStatusFiltersReturnOnlyMatchingRequests()
    {
        var harness = await CreateHarnessAsync();
        var fresh = await harness.CreateRequestAsync("H5");
        var inProgress = await harness.CreateRequestAsync("C5");
        var resolved = await harness.CreateRequestAsync("E6");
        var cancelled = await harness.CreateRequestAsync("F5");
        await harness.RequestStore.UpdateAsync(Update(
            inProgress.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.Engineer.Id));
        await harness.RequestStore.UpdateAsync(Update(
            resolved.Id,
            TelegramServiceRequestStatus.Resolved,
            harness.Engineer.Id));
        await harness.RequestStore.UpdateAsync(Update(
            cancelled.Id,
            TelegramServiceRequestStatus.Cancelled,
            harness.Admin.Id));

        var newOnly = await harness.HandleAsync("/queue new", harness.Admin);
        var inProgressOnly = await harness.HandleAsync("/queue in-progress", harness.Admin);
        var closed = await harness.HandleAsync("/queue closed", harness.Admin);
        var all = await harness.HandleAsync("/queue all", harness.Admin);

        Assert.Contains($"#{fresh.Id}", newOnly, StringComparison.Ordinal);
        Assert.DoesNotContain($"#{inProgress.Id}", newOnly, StringComparison.Ordinal);
        Assert.Contains($"#{inProgress.Id}", inProgressOnly, StringComparison.Ordinal);
        Assert.DoesNotContain($"#{fresh.Id}", inProgressOnly, StringComparison.Ordinal);
        Assert.Contains($"#{resolved.Id}", closed, StringComparison.Ordinal);
        Assert.Contains($"#{cancelled.Id}", closed, StringComparison.Ordinal);
        Assert.DoesNotContain($"#{fresh.Id}", closed, StringComparison.Ordinal);
        Assert.Contains($"#{fresh.Id}", all, StringComparison.Ordinal);
        Assert.Contains($"#{inProgress.Id}", all, StringComparison.Ordinal);
        Assert.Contains($"#{resolved.Id}", all, StringComparison.Ordinal);
        Assert.Contains($"#{cancelled.Id}", all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MyRequestsShowsOnlyAssignedActiveRequests()
    {
        var harness = await CreateHarnessAsync();
        var mine = await harness.CreateRequestAsync("H5");
        var other = await harness.CreateRequestAsync("C5");
        var closedMine = await harness.CreateRequestAsync("E6");
        await harness.RequestStore.UpdateAsync(Update(
            mine.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.Engineer.Id));
        await harness.RequestStore.UpdateAsync(Update(
            other.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.OtherEngineer.Id));
        await harness.RequestStore.UpdateAsync(Update(
            closedMine.Id,
            TelegramServiceRequestStatus.Resolved,
            harness.Engineer.Id));

        var result = await harness.HandleAsync("/my_requests", harness.Engineer);

        Assert.Contains($"#{mine.Id}", result, StringComparison.Ordinal);
        Assert.DoesNotContain($"#{other.Id}", result, StringComparison.Ordinal);
        Assert.DoesNotContain($"#{closedMine.Id}", result, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConsumerCannotUseQueueOrMyRequests()
    {
        var harness = await CreateHarnessAsync();

        var queue = await harness.HandleAsync("/queue all", harness.Consumer);
        var mine = await harness.HandleAsync("/my_requests", harness.Consumer);

        Assert.Equal("Команда недоступна.", queue);
        Assert.Equal("Команда недоступна.", mine);
    }

    [Fact]
    public async Task InstallerCannotUseServiceCommandsCallbacksOrReceiveContact()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        var commands = new[]
        {
            "/queue",
            "/queue active",
            "/queue new",
            "/queue in-progress",
            "/queue closed",
            "/queue all",
            "/my_requests",
            $"/take {request.Id}",
            $"/assign {request.Id} @engineer",
            $"/done {request.Id}",
            $"/cancel_request {request.Id}",
            $"/contact {request.Id}",
            $"/request_events {request.Id}"
        };

        foreach (var command in commands)
        {
            Assert.Equal("Команда недоступна.", await harness.HandleAsync(command, harness.Installer));
        }

        var callback = await harness.HandleCallbackAsync(
            $"sr:c:{request.Id}",
            harness.Installer,
            messageId: 777);
        var queueCallback = await harness.HandleCallbackAsync(
            "sq:a",
            harness.Installer,
            messageId: 778);

        Assert.Equal("Нет доступа", callback.CallbackAnswerText);
        Assert.Equal("Нет доступа", queueCallback.CallbackAnswerText);
        Assert.DoesNotContain(harness.Outbound.Messages, message =>
            message.ChatId == harness.Installer.TelegramChatId &&
            message.Text.Contains(FullPhone, StringComparison.Ordinal));
        Assert.Null((await harness.RequestStore.GetByIdAsync(request.Id))?.AssignedTelegramUserId);
    }

    [Fact]
    public async Task QueueResponseIncludesInlineActionsForActiveRequests()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCommandResultAsync("/queue", harness.Engineer);
        var buttons = InlineButtons(result);

        Assert.Contains(buttons, button => button.Text == $"Открыть #{request.Id}" && button.CallbackData == $"sr:o:{request.Id}");
        Assert.Contains(buttons, button => button.Text == "Активные" && button.CallbackData == "sq:a");
        Assert.Contains(buttons, button => button.Text == "Новые" && button.CallbackData == "sq:n");
        Assert.Contains(buttons, button => button.Text == "В работе" && button.CallbackData == "sq:p");
        Assert.Contains(buttons, button => button.Text == "Мои" && button.CallbackData == "sq:m");
        Assert.Contains(buttons, button => button.Text == "Закрытые" && button.CallbackData == "sq:c");
        Assert.Contains(buttons, button => button.Text == "Все" && button.CallbackData == "sq:l");
    }

    [Fact]
    public async Task QueueOpenRendersActiveRequestActionsInSameMessage()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCallbackAsync($"sr:o:{request.Id}", harness.Admin, messageId: 777);

        Assert.Equal("Заявка открыта", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
        var edit = Assert.Single(harness.Outbound.Edits);
        var buttons = InlineButtons(edit.ReplyMarkup);
        Assert.Contains(buttons, button => button.Text == "Взять в работу");
        Assert.Contains(buttons, button => button.Text == "Статус");
        Assert.Contains(buttons, button => button.Text == "Контакт");
        Assert.Contains(buttons, button => button.Text == "История");
        Assert.Contains(buttons, button => button.Text == "Отменить");
        Assert.Contains(buttons, button => button.Text == "К активной очереди" && button.CallbackData == "sq:a");
        Assert.All(buttons, button => Assert.DoesNotContain(FullPhone, button.CallbackData, StringComparison.Ordinal));
        Assert.DoesNotContain(FullPhone, edit.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueueActionStatusChangeRefreshesLiveCardAndQueueActionMessage()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.HandleCallbackAsync($"sr:o:{request.Id}", harness.Engineer, messageId: 777);
        harness.Outbound.Edits.Clear();

        var result = await harness.HandleCallbackAsync($"sr:t:{request.Id}", harness.Engineer, messageId: 777);

        Assert.Equal("Готово", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
        Assert.Contains(harness.Outbound.Edits, edit =>
            edit.MessageId == 9000 + request.Id &&
            edit.Text.Contains("Статус: в работе", StringComparison.Ordinal));
        Assert.Contains(harness.Outbound.Edits, edit =>
            edit.MessageId == 777 &&
            edit.Text.Contains("Статус: в работе", StringComparison.Ordinal));
    }

    [Fact]
    public async Task QueueActionEditFailureReturnsSafeReplacementCard()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        harness.Outbound.EditSucceeds = false;

        var result = await harness.HandleCallbackAsync($"sr:t:{request.Id}", harness.Engineer, messageId: 777);

        Assert.False(result.SuppressGroupMessage);
        Assert.Contains("Статус: в работе", result.Text, StringComparison.Ordinal);
        Assert.Contains(InlineButtons(result), button => button.Text == "Закрыть");
        Assert.DoesNotContain(FullPhone, result.Text, StringComparison.Ordinal);
        Assert.Contains(harness.Outbound.Messages, message =>
            message.ChatId == ServiceGroupId &&
            message.Text.Contains("Статус: в работе", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(TelegramServiceRequestStatus.Resolved)]
    [InlineData(TelegramServiceRequestStatus.Cancelled)]
    public async Task QueueOpenForClosedRequestDoesNotRenderActiveActions(
        TelegramServiceRequestStatus status)
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.RequestStore.UpdateAsync(Update(request.Id, status, harness.Engineer.Id));

        await harness.HandleCallbackAsync($"sr:o:{request.Id}", harness.Admin, messageId: 777);

        var buttons = InlineButtons(Assert.Single(harness.Outbound.Edits).ReplyMarkup);
        Assert.Contains(buttons, button => button.Text == "Статус");
        Assert.Contains(buttons, button => button.Text == "История");
        Assert.DoesNotContain(buttons, button => button.Text is
            "Взять в работу" or "Назначить" or "Контакт" or "Закрыть" or "Отменить");
    }

    [Fact]
    public async Task TerminalAndAssignedLifecycleGuardsRejectStaleActions()
    {
        var harness = await CreateHarnessAsync();
        var resolved = await harness.CreateRequestAsync("H5");
        var cancelled = await harness.CreateRequestAsync("C5");
        var assigned = await harness.CreateRequestAsync("E6");
        await harness.RequestStore.UpdateAsync(Update(
            resolved.Id,
            TelegramServiceRequestStatus.Resolved,
            harness.Engineer.Id));
        await harness.RequestStore.UpdateAsync(Update(
            cancelled.Id,
            TelegramServiceRequestStatus.Cancelled,
            harness.Engineer.Id));
        await harness.RequestStore.UpdateAsync(Update(
            assigned.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.OtherEngineer.Id));

        var takeResolved = await harness.HandleAsync($"/take {resolved.Id}", harness.Engineer);
        var takeCancelled = await harness.HandleAsync($"/take {cancelled.Id}", harness.Engineer);
        var cancelResolved = await harness.HandleAsync($"/cancel_request {resolved.Id}", harness.Admin);
        var resolveCancelled = await harness.HandleAsync($"/done {cancelled.Id}", harness.Admin);
        var takeAssigned = await harness.HandleAsync($"/take {assigned.Id}", harness.Engineer);

        Assert.Contains("уже закрыта", takeResolved, StringComparison.Ordinal);
        Assert.Contains("отменена, действие недоступно", takeCancelled, StringComparison.Ordinal);
        Assert.Contains("уже закрыта", cancelResolved, StringComparison.Ordinal);
        Assert.Contains("отменена, действие недоступно", resolveCancelled, StringComparison.Ordinal);
        Assert.Contains("уже назначена другому инженеру", takeAssigned, StringComparison.Ordinal);
        Assert.Equal(harness.OtherEngineer.Id, (await harness.RequestStore.GetByIdAsync(assigned.Id))?.AssignedTelegramUserId);
    }

    [Fact]
    public async Task UnauthorizedContactRequestsDoNotLeakPhone()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.RequestStore.UpdateAsync(Update(
            request.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.Engineer.Id));

        var wrongEngineer = await harness.HandleAsync($"/contact {request.Id}", harness.OtherEngineer);
        var consumer = await harness.HandleAsync($"/contact {request.Id}", harness.Consumer);

        Assert.Contains("только назначенному инженеру", wrongEngineer, StringComparison.Ordinal);
        Assert.Equal("Команда недоступна.", consumer);
        Assert.DoesNotContain(FullPhone, wrongEngineer, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, consumer, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Outbound.Messages, item =>
            item.ChatId is 21 or 40 && item.Text.Contains(FullPhone, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RequestActionDatabaseFailureReturnsSafeCallbackFallback()
    {
        var harness = await CreateHarnessAsync(requestStore: new ThrowingRequestActionStore());

        var result = await harness.HandleCallbackAsync("sr:t:1", harness.Engineer);

        Assert.Equal("Действие временно недоступно. Попробуйте позже.", result.Text);
        Assert.Equal("Действие временно недоступно. Попробуйте позже.", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
        Assert.Contains(harness.Logger.Messages, message =>
            message.Contains("request callback failed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(harness.Logger.Messages, message =>
            message.Contains(FullPhone, StringComparison.Ordinal) ||
            message.Contains(ServiceGroupId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task QueueFilterCallbackEditsMessageAndReturnsCallbackAnswer()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCallbackAsync("sq:n", harness.Admin);

        Assert.Equal("Очередь обновлена", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
        var edit = Assert.Single(harness.Outbound.Edits);
        Assert.Equal(777, edit.MessageId);
        Assert.Contains($"#{request.Id}", edit.Text, StringComparison.Ordinal);
        Assert.Contains("Новые сервисные заявки", edit.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, edit.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sq:")]
    [InlineData("sq:unknown")]
    [InlineData("sq:a:extra")]
    public async Task MalformedQueueCallbackIsHandledSafely(string data)
    {
        var harness = await CreateHarnessAsync();

        var result = await harness.HandleCallbackAsync(data, harness.Engineer);

        Assert.Equal("Действие недоступно.", result.Text);
        Assert.Equal("Действие недоступно.", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
        Assert.Empty(harness.Outbound.Edits);
    }

    [Fact]
    public async Task QueueDatabaseFailureReturnsSafeFallback()
    {
        var harness = await CreateHarnessAsync(requestStore: new ThrowingQueueRequestStore());

        var command = await harness.HandleCommandResultAsync("/queue all", harness.Admin);
        var callback = await harness.HandleCallbackAsync("sq:l", harness.Admin);

        Assert.Equal("Очередь временно недоступна. Попробуйте позже.", command.Text);
        Assert.Equal("Очередь временно недоступна. Попробуйте позже.", callback.CallbackAnswerText);
        Assert.True(callback.SuppressGroupMessage);
        Assert.DoesNotContain(FullPhone, command.Text, StringComparison.Ordinal);
        Assert.Contains(harness.Logger.Messages, message =>
            message.Contains("queue query failed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(harness.Logger.Messages, message =>
            message.Contains(FullPhone, StringComparison.Ordinal) ||
            message.Contains(ServiceGroupId.ToString(), StringComparison.Ordinal));
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
        Assert.Contains(InlineButtons(edit.ReplyMarkup), button => button.Text == "История");
        Assert.DoesNotContain(harness.Outbound.Messages, item => item.ChatId == ServiceGroupId);
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 20),
            item => item.EventType == TelegramServiceRequestEventType.Taken &&
                item.ActorTelegramUserId == harness.Engineer.Id);
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
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 20),
            item => item.EventType == TelegramServiceRequestEventType.Assigned &&
                item.ActorTelegramUserId == harness.Admin.Id &&
                item.TargetTelegramUserId == harness.Engineer.Id);
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
        var events = await harness.EventStore.GetLatestAsync(request.Id, 50);
        Assert.Contains(events, item => item.EventType == TelegramServiceRequestEventType.ContactRequested);
        Assert.Contains(events, item => item.EventType == TelegramServiceRequestEventType.ContactSent);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.ContactDenied &&
            item.ActorTelegramUserId == harness.OtherEngineer.Id);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.ContactSent &&
            item.MetadataJson == "{\"contact_delivered\":true}");
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
            InlineButtons(edit.ReplyMarkup).Select(button => button.CallbackData).Order().SequenceEqual(
                new[] { $"sr:e:{first.Id}", $"sr:reply:{first.Id}", $"sr:s:{first.Id}", $"sr:thread:{first.Id}" }.Order()));
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(first.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.Resolved);
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(second.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.Cancelled);
        Assert.Contains(harness.Outbound.Edits, edit =>
            edit.Text.Contains("Статус: отменена", StringComparison.Ordinal) &&
            InlineButtons(edit.ReplyMarkup).Select(button => button.CallbackData).Order().SequenceEqual(
                new[] { $"sr:e:{second.Id}", $"sr:reply:{second.Id}", $"sr:s:{second.Id}", $"sr:thread:{second.Id}" }.Order()));
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

        Assert.Equal("Действие недоступно.", result.Text);
        Assert.Equal("Действие недоступно.", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
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
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 20),
            item => item.EventType == TelegramServiceRequestEventType.Taken);
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
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 30),
            item => item.EventType == TelegramServiceRequestEventType.Assigned);
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
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.Resolved);
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(second.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.Cancelled);
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
        var events = await harness.EventStore.GetLatestAsync(request.Id, 50);
        Assert.Contains(events, item => item.EventType == TelegramServiceRequestEventType.ContactRequested);
        Assert.Contains(events, item => item.EventType == TelegramServiceRequestEventType.ContactSent);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.ContactDenied &&
            item.ActorTelegramUserId == harness.OtherEngineer.Id);
    }

    [Fact]
    public async Task ConsumerContactRequestWritesDeniedAuditWithoutSensitiveData()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        var response = await harness.HandleAsync($"/contact {request.Id}", harness.Consumer);

        Assert.Equal("Команда недоступна.", response);
        var events = await harness.EventStore.GetLatestAsync(request.Id, 50);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.ContactRequested &&
            item.ActorTelegramUserId == harness.Consumer.Id);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.ContactDenied &&
            item.ActorTelegramUserId == harness.Consumer.Id);
        Assert.All(events, item =>
            Assert.DoesNotContain(FullPhone, string.Concat(item.Message, item.MetadataJson), StringComparison.Ordinal));
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
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.ContactFailed &&
                !item.IsSuccessful &&
                !string.Concat(item.Message, item.MetadataJson).Contains(FullPhone, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CustomerNotificationFailureDoesNotRollbackStatus()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        harness.Outbound.FailingChatIds.Add(harness.Customer.TelegramChatId);

        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        Assert.Equal(TelegramServiceRequestStatus.InProgress, (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.CustomerNotificationFailed &&
                !item.IsSuccessful);
    }

    [Fact]
    public async Task ReassignWritesReassignedEvent()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");

        await harness.HandleAsync($"/assign {request.Id} @engineer", harness.Admin);
        await harness.HandleAsync($"/assign {request.Id} @otherengineer", harness.Admin);

        Assert.Contains(
            await harness.EventStore.GetLatestAsync(request.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.Reassigned &&
                item.ActorTelegramUserId == harness.Admin.Id &&
                item.TargetTelegramUserId == harness.OtherEngineer.Id);
    }

    [Fact]
    public async Task RequestEventsUsesLocalTimeIsPrivateSafeAndEnforcesAccess()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.EventStore.AppendAsync(new TelegramServiceRequestEventCreate(
            request.Id,
            TelegramServiceRequestEventType.Created,
            harness.Customer.Id,
            null,
            null,
            TelegramServiceRequestStatus.New,
            true,
            "safe",
            null,
            FixedNowUtc));
        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        var assigned = await harness.HandleAsync($"/request_events {request.Id}", harness.Engineer);
        var admin = await harness.HandleAsync($"/request_events {request.Id}", harness.Admin);
        var deniedEngineer = await harness.HandleAsync($"/request_events {request.Id}", harness.OtherEngineer);
        var consumer = await harness.HandleAsync($"/request_events {request.Id}", harness.Consumer);

        Assert.Contains($"История заявки #{request.Id}", assigned, StringComparison.Ordinal);
        Assert.Contains("18.06.2026 17:20", assigned, StringComparison.Ordinal);
        Assert.Contains("@customer", assigned, StringComparison.Ordinal);
        Assert.Contains("@engineer", assigned, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, assigned, StringComparison.Ordinal);
        Assert.DoesNotContain(ServiceGroupId.ToString(), assigned, StringComparison.Ordinal);
        Assert.Contains("История заявки", admin, StringComparison.Ordinal);
        Assert.Contains("история открыта", admin, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("только назначенному инженеру", deniedEngineer, StringComparison.Ordinal);
        Assert.Equal("Команда недоступна.", consumer);
        var events = await harness.EventStore.GetLatestAsync(request.Id, 100);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.HistoryViewed &&
            item.ActorTelegramUserId == harness.Engineer.Id);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.HistoryViewed &&
            item.ActorTelegramUserId == harness.Admin.Id);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.HistoryDenied &&
            item.ActorTelegramUserId == harness.OtherEngineer.Id);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.HistoryDenied &&
            item.ActorTelegramUserId == harness.Consumer.Id);
    }

    [Fact]
    public async Task HistoryCallbackReturnsCompactHistoryAndCallbackAnswer()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.EventStore.AppendAsync(new TelegramServiceRequestEventCreate(
            request.Id,
            TelegramServiceRequestEventType.Created,
            harness.Customer.Id,
            null,
            null,
            TelegramServiceRequestStatus.New,
            true,
            null,
            null,
            FixedNowUtc));
        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        var result = await harness.HandleCallbackAsync($"sr:e:{request.Id}", harness.Engineer);
        var denied = await harness.HandleCallbackAsync($"sr:e:{request.Id}", harness.OtherEngineer);

        Assert.Equal("История загружена", result.CallbackAnswerText);
        Assert.False(result.SuppressGroupMessage);
        Assert.Contains("История заявки", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, result.Text, StringComparison.Ordinal);
        Assert.Contains("только назначенному инженеру", denied.Text, StringComparison.Ordinal);
        Assert.Equal("Нет доступа", denied.CallbackAnswerText);
        Assert.True(denied.SuppressGroupMessage);
        var events = await harness.EventStore.GetLatestAsync(request.Id, 100);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.HistoryViewed &&
            item.ActorTelegramUserId == harness.Engineer.Id);
        Assert.Contains(events, item =>
            item.EventType == TelegramServiceRequestEventType.HistoryDenied &&
            item.ActorTelegramUserId == harness.OtherEngineer.Id);
    }

    [Fact]
    public async Task DeniedLifecycleActionWritesSafeActionDeniedMetadata()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        await harness.RequestStore.UpdateAsync(Update(
            request.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.OtherEngineer.Id));

        var response = await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        Assert.Contains("назначена другому инженеру", response, StringComparison.Ordinal);
        var denied = Assert.Single(
            await harness.EventStore.GetLatestAsync(request.Id, 50),
            item => item.EventType == TelegramServiceRequestEventType.ActionDenied);
        Assert.Equal("{\"action\":\"take\",\"reason\":\"assigned_to_another_engineer\"}", denied.MetadataJson);
        Assert.DoesNotContain(FullPhone, string.Concat(denied.Message, denied.MetadataJson), StringComparison.Ordinal);
        Assert.DoesNotContain(ServiceGroupId.ToString(), string.Concat(denied.Message, denied.MetadataJson), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestEventsDatabaseFailureReturnsSafeFallbackWithoutThrowing()
    {
        var eventStore = new ThrowingEventStore(throwOnRead: true);
        var harness = await CreateHarnessAsync(eventStore);
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCommandResultAsync($"/request_events {request.Id}", harness.Admin);

        Assert.Equal("История временно недоступна. Попробуйте позже.", result.Text);
        Assert.DoesNotContain(FullPhone, result.Text, StringComparison.Ordinal);
        Assert.Contains(harness.Logger.Messages, message =>
            message.Contains("history query failed", StringComparison.OrdinalIgnoreCase) &&
            message.Contains($"RequestId: {request.Id}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HistoryCallbackDatabaseFailureReturnsSafeAnswerWithoutThrowing()
    {
        var eventStore = new ThrowingEventStore(throwOnRead: true);
        var harness = await CreateHarnessAsync(eventStore);
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCallbackAsync($"sr:e:{request.Id}", harness.Admin);

        Assert.Equal("История временно недоступна. Попробуйте позже.", result.Text);
        Assert.Equal("История временно недоступна. Попробуйте позже.", result.CallbackAnswerText);
        Assert.True(result.SuppressGroupMessage);
        Assert.DoesNotContain(FullPhone, result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditEventWriteFailureDoesNotBlockInlineAssign()
    {
        var harness = await CreateHarnessAsync(new ThrowingEventStore(throwOnAppend: true));
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCallbackAsync(
            $"sr:as:{request.Id}:{harness.Engineer.Id}",
            harness.Admin);

        Assert.Contains("@engineer", result.Text, StringComparison.Ordinal);
        Assert.Equal(
            harness.Engineer.Id,
            (await harness.RequestStore.GetByIdAsync(request.Id))?.AssignedTelegramUserId);
    }

    [Fact]
    public async Task AuditEventWriteFailureDoesNotBlockInlineDone()
    {
        var harness = await CreateHarnessAsync(new ThrowingEventStore(throwOnAppend: true));
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleCallbackAsync($"sr:d:{request.Id}", harness.Admin);

        Assert.Contains("закрыта", result.Text, StringComparison.Ordinal);
        Assert.Equal(
            TelegramServiceRequestStatus.Resolved,
            (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
    }

    [Fact]
    public async Task AuditEventWriteFailureDoesNotBlockCancelCommand()
    {
        var harness = await CreateHarnessAsync(new ThrowingEventStore(throwOnAppend: true));
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleAsync($"/cancel_request {request.Id}", harness.Admin);

        Assert.Contains("отменена", result, StringComparison.Ordinal);
        Assert.Equal(
            TelegramServiceRequestStatus.Cancelled,
            (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
    }

    [Fact]
    public async Task AuditEventWriteFailureDoesNotBlockTakeCommand()
    {
        var harness = await CreateHarnessAsync(new ThrowingEventStore(throwOnAppend: true));
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        Assert.Contains("взята в работу", result, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            TelegramServiceRequestStatus.InProgress,
            (await harness.RequestStore.GetByIdAsync(request.Id))?.Status);
    }

    [Fact]
    public async Task AuditEventWriteFailureDoesNotBlockContactCommand()
    {
        var harness = await CreateHarnessAsync(new ThrowingEventStore(throwOnAppend: true));
        var request = await harness.CreateRequestAsync("H5");
        await harness.RequestStore.UpdateAsync(Update(
            request.Id,
            TelegramServiceRequestStatus.InProgress,
            harness.Engineer.Id));
        harness.Outbound.Messages.Clear();

        var result = await harness.HandleAsync($"/contact {request.Id}", harness.Engineer);

        Assert.Equal("Контакт отправлен в личный чат.", result);
        Assert.Contains(harness.Outbound.Messages, message =>
            message.ChatId == harness.Engineer.TelegramChatId &&
            message.Text.Contains(FullPhone, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuditEventWriteFailureDoesNotBlockHistoryDisplay()
    {
        var harness = await CreateHarnessAsync(new ThrowingEventStore(throwOnAppend: true));
        var request = await harness.CreateRequestAsync("H5");

        var result = await harness.HandleAsync($"/request_events {request.Id}", harness.Admin);

        Assert.Contains($"История заявки #{request.Id}", result, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditEventWriteFailureLogIsSanitized()
    {
        var eventLogger = new CapturingLogger<TelegramServiceRequestEventService>();
        var eventStore = new ThrowingEventStore(
            throwOnAppend: true,
            exceptionMessage: $"phone={FullPhone};chat={ServiceGroupId};callback=sr:t:1");
        var harness = await CreateHarnessAsync(eventStore, eventLogger);
        var request = await harness.CreateRequestAsync("H5");

        await harness.HandleAsync($"/take {request.Id}", harness.Engineer);

        Assert.NotEmpty(harness.EventLogger.Messages);
        Assert.All(harness.EventLogger.Messages, message =>
        {
            Assert.Contains("audit event write failed", message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"RequestId: {request.Id}", message, StringComparison.Ordinal);
            Assert.DoesNotContain(FullPhone, message, StringComparison.Ordinal);
            Assert.DoesNotContain(ServiceGroupId.ToString(), message, StringComparison.Ordinal);
            Assert.DoesNotContain("sr:t:1", message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task EventServiceDropsSensitiveMessageAndMetadata()
    {
        var harness = await CreateHarnessAsync();
        var request = await harness.CreateRequestAsync("H5");
        var service = new TelegramServiceRequestEventService(
            harness.EventStore,
            harness.UserStore,
            harness.TimeFormatter);

        await service.AppendSafeAsync(new TelegramServiceRequestEventCreate(
            request.Id,
            TelegramServiceRequestEventType.ContactSent,
            harness.Admin.Id,
            harness.Engineer.Id,
            null,
            null,
            true,
            $"phone={FullPhone};chat={ServiceGroupId}",
            $"{{\"token\":\"secret\",\"phone\":\"{FullPhone}\"}}",
            FixedNowUtc));

        var item = Assert.Single(
            await harness.EventStore.GetLatestAsync(request.Id, 50),
            value => value.EventType == TelegramServiceRequestEventType.ContactSent);
        var stored = string.Concat(item.Message, item.MetadataJson);
        Assert.DoesNotContain(FullPhone, stored, StringComparison.Ordinal);
        Assert.DoesNotContain(ServiceGroupId.ToString(), stored, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", stored, StringComparison.Ordinal);
        Assert.Null(item.MetadataJson);
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

    private static async Task<Harness> CreateHarnessAsync(
        ITelegramServiceRequestEventStore? eventStore = null,
        CapturingLogger<TelegramServiceRequestEventService>? eventLogger = null,
        ITelegramServiceRequestStore? requestStore = null)
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
        var installer = await CreateUserAsync(users, 31, 3131, "installer", TelegramUserRole.Installer);
        var consumer = await CreateUserAsync(users, 40, 4040, "consumer", TelegramUserRole.Consumer);
        var requests = requestStore ?? new InMemoryTelegramServiceRequestStore();
        var events = eventStore ?? new InMemoryTelegramServiceRequestEventStore();
        var history = new InMemoryTelegramDiagnosticCaseStore();
        var outbound = new FakeOutbound();
        var formatter = new TelegramDisplayTimeFormatter(options, new FixedTimeProvider());
        var logger = new CapturingLogger<TelegramServiceRequestQueueService>();
        eventLogger ??= new CapturingLogger<TelegramServiceRequestEventService>();
        var eventService = new TelegramServiceRequestEventService(events, users, formatter, eventLogger);
        var service = new TelegramServiceRequestQueueService(
            requests,
            users,
            outbound,
            options,
            formatter,
            new TelegramServiceRequestCardRenderer(users, formatter),
            logger,
            eventService);
        return new Harness(options, service, users, requests, events, history, outbound, formatter, logger, eventLogger, customer, engineer, otherEngineer, admin, installer, consumer);
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
        ITelegramServiceRequestStore RequestStore,
        ITelegramServiceRequestEventStore EventStore,
        InMemoryTelegramDiagnosticCaseStore HistoryStore,
        FakeOutbound Outbound,
        TelegramDisplayTimeFormatter TimeFormatter,
        CapturingLogger<TelegramServiceRequestQueueService> Logger,
        CapturingLogger<TelegramServiceRequestEventService> EventLogger,
        TelegramUserSnapshot Customer,
        TelegramUserSnapshot Engineer,
        TelegramUserSnapshot OtherEngineer,
        TelegramUserSnapshot Admin,
        TelegramUserSnapshot Installer,
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
            long chatId = ServiceGroupId,
            long? messageId = null)
        {
            var resolvedMessageId = messageId ?? CallbackMessageId(data);
            return
            Service.HandleCallbackAsync(
                new EquipmentDiagnosticTelegramUpdate(
                    1,
                    chatId,
                    sender.Username,
                    Text: null,
                    MessageId: resolvedMessageId,
                    ReceivedAt: FixedNowUtc,
                    UserId: sender.TelegramUserId,
                    ChatType: "supergroup",
                    CallbackQueryId: "callback-id",
                    CallbackData: data));
        }

        private static long CallbackMessageId(string? data)
        {
            if (data?.StartsWith("sq:", StringComparison.Ordinal) == true ||
                data?.StartsWith("sr:o:", StringComparison.Ordinal) == true)
            {
                return 777;
            }

            var parts = data?.Split(':');
            return parts is { Length: >= 3 } && long.TryParse(parts[2], out var requestId)
                ? 9000 + requestId
                : 777;
        }

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

    private sealed class ThrowingEventStore(
        bool throwOnAppend = false,
        bool throwOnRead = false,
        string exceptionMessage = "database unavailable")
        : ITelegramServiceRequestEventStore
    {
        public Task<TelegramServiceRequestEventSnapshot> AppendAsync(
            TelegramServiceRequestEventCreate request,
            CancellationToken cancellationToken = default) =>
            throwOnAppend
                ? Task.FromException<TelegramServiceRequestEventSnapshot>(
                    new InvalidOperationException(exceptionMessage))
                : Task.FromResult(new TelegramServiceRequestEventSnapshot(
                    1,
                    request.ServiceRequestId,
                    request.EventType,
                    request.ActorTelegramUserId,
                    request.TargetTelegramUserId,
                    request.OldStatus,
                    request.NewStatus,
                    request.IsSuccessful,
                    request.Message,
                    request.MetadataJson,
                    request.CreatedAt));

        public Task<IReadOnlyList<TelegramServiceRequestEventSnapshot>> GetLatestAsync(
            long serviceRequestId,
            int limit,
            CancellationToken cancellationToken = default) =>
            throwOnRead
                ? Task.FromException<IReadOnlyList<TelegramServiceRequestEventSnapshot>>(
                    new InvalidOperationException(exceptionMessage))
                : Task.FromResult<IReadOnlyList<TelegramServiceRequestEventSnapshot>>([]);
    }

    private sealed class ThrowingQueueRequestStore : ITelegramServiceRequestStore
    {
        public Task<TelegramServiceRequestCreateResult> CreateIfNoActiveAsync(
            TelegramServiceRequestCreate request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestForTelegramUserAsync(
            long telegramUserId,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TelegramServiceRequestSnapshot?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetActiveAsync(
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestAsync(
            IReadOnlyCollection<TelegramServiceRequestStatus>? statuses,
            long? assignedTelegramUserId,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<TelegramServiceRequestSnapshot>>(
                new InvalidOperationException($"phone={FullPhone};chat={ServiceGroupId}"));

        public Task<TelegramServiceRequestSnapshot?> UpdateAsync(
            TelegramServiceRequestUpdate update,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TelegramServiceRequestSnapshot?> UpdateNotificationAsync(
            TelegramServiceRequestNotificationUpdate update,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingRequestActionStore : ITelegramServiceRequestStore
    {
        public Task<TelegramServiceRequestCreateResult> CreateIfNoActiveAsync(
            TelegramServiceRequestCreate request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestForTelegramUserAsync(
            long telegramUserId,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TelegramServiceRequestSnapshot?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default) =>
            Task.FromException<TelegramServiceRequestSnapshot?>(
                new InvalidOperationException($"phone={FullPhone};chat={ServiceGroupId}"));

        public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetActiveAsync(
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestAsync(
            IReadOnlyCollection<TelegramServiceRequestStatus>? statuses,
            long? assignedTelegramUserId,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TelegramServiceRequestSnapshot?> UpdateAsync(
            TelegramServiceRequestUpdate update,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TelegramServiceRequestSnapshot?> UpdateNotificationAsync(
            TelegramServiceRequestNotificationUpdate update,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
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
