using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramAdminUserManagementTests
{
    private const string FullPhone = "+998901234567";
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 6, 18, 15, 10, 0, TimeSpan.Zero);

    [Fact]
    public async Task AdminUsersIsDeniedForConsumerAndAllowedForOwnerAndAdmin()
    {
        var harness = await CreateHarnessAsync();

        var consumer = await harness.Adapter.HandleAsync(Command("/admin_users", harness.Consumer));
        var owner = await harness.Adapter.HandleAsync(Command("/admin_users", harness.Owner));
        var admin = await harness.Adapter.HandleAsync(Command("/admin_users", harness.Admin));

        Assert.Equal("Команда недоступна.", consumer.Text);
        Assert.Contains("Пользователи Telegram", owner.Text, StringComparison.Ordinal);
        Assert.Contains("Пользователи Telegram", admin.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, owner.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Consumer.TelegramChatId.ToString(), owner.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Consumer.TelegramUserId!.Value.ToString(), owner.Text, StringComparison.Ordinal);
        Assert.NotEmpty(Buttons(owner));
    }

    [Fact]
    public async Task PendingAndEngineersListsContainExpectedUsersAndActions()
    {
        var harness = await CreateHarnessAsync();

        var pending = await harness.Adapter.HandleAsync(Command("/admin_pending", harness.Owner));
        var engineers = await harness.Adapter.HandleAsync(Command("/engineers", harness.Admin));

        Assert.Contains("@consumer", pending.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("@engineer", pending.Text, StringComparison.Ordinal);
        Assert.Contains(Buttons(pending), button => button.CallbackData == $"au:v:{harness.Consumer.Id}");
        Assert.Contains("@engineer", engineers.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("@consumer", engineers.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UserDetailIsPrivateSafeAndShowsCurrentState()
    {
        var harness = await CreateHarnessAsync();

        var result = await harness.Service.HandleCallbackAsync(
            Callback($"au:v:{harness.Consumer.Id}", harness.Owner));

        Assert.Contains("Пользователь Telegram", result.Text, StringComparison.Ordinal);
        Assert.Contains("Роль: Клиент", result.Text, StringComparison.Ordinal);
        Assert.Contains("Доступ: включён", result.Text, StringComparison.Ordinal);
        Assert.Contains("Блокировка: нет", result.Text, StringComparison.Ordinal);
        Assert.Contains("Телефон: сохранён", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Consumer.TelegramChatId.ToString(), result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Consumer.TelegramUserId!.Value.ToString(), result.Text, StringComparison.Ordinal);
        Assert.Contains(Buttons(result.ReplyMarkup), button => button.Text == "Сделать сервис-инженером");
        Assert.Contains(Buttons(result.ReplyMarkup), button => button.Text == "Сделать монтажником");
        Assert.Contains(Buttons(result.ReplyMarkup), button => button.Text == "Сделать админом");
        Assert.All(Buttons(result.ReplyMarkup), button =>
        {
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(button.CallbackData) <= 64);
            Assert.DoesNotContain(FullPhone, button.CallbackData, StringComparison.Ordinal);
            Assert.DoesNotContain("consumer", button.CallbackData, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task OwnerCanPromoteConsumerToEngineerAndAdmin()
    {
        var engineerHarness = await CreateHarnessAsync();

        var engineer = await engineerHarness.Service.HandleCallbackAsync(
            Callback($"au:r:{engineerHarness.Consumer.Id}:e", engineerHarness.Owner));

        Assert.Equal("Роль обновлена", engineer.CallbackAnswerText);
        Assert.Equal(
            TelegramUserRole.Engineer,
            (await engineerHarness.Users.GetByIdAsync(engineerHarness.Consumer.Id))?.Role);
        Assert.Contains("Роль: Сервис-инженер", Assert.Single(engineerHarness.Outbound.Edits).Text, StringComparison.Ordinal);

        var adminHarness = await CreateHarnessAsync();
        await adminHarness.Service.HandleCallbackAsync(
            Callback($"au:r:{adminHarness.Consumer.Id}:a", adminHarness.Owner));
        Assert.Equal(
            TelegramUserRole.Admin,
            (await adminHarness.Users.GetByIdAsync(adminHarness.Consumer.Id))?.Role);
    }

    [Fact]
    public async Task OwnerAndAdminCanAssignInstallerAndAuditRoleTransitions()
    {
        var ownerHarness = await CreateHarnessAsync();

        await ownerHarness.Service.HandleCallbackAsync(
            Callback($"au:r:{ownerHarness.Consumer.Id}:i", ownerHarness.Owner));
        await ownerHarness.Service.HandleCallbackAsync(
            Callback($"au:r:{ownerHarness.Consumer.Id}:c", ownerHarness.Owner));
        await ownerHarness.Service.HandleCallbackAsync(
            Callback($"au:r:{ownerHarness.Consumer.Id}:i", ownerHarness.Admin));
        await ownerHarness.Service.HandleCallbackAsync(
            Callback($"au:r:{ownerHarness.Consumer.Id}:e", ownerHarness.Admin));

        Assert.Equal(
            TelegramUserRole.Engineer,
            (await ownerHarness.Users.GetByIdAsync(ownerHarness.Consumer.Id))?.Role);
        var roleEvents = (await ownerHarness.AuditStore.GetLatestAsync(10))
            .Where(item => item.EventType == TelegramUserAuditEventType.RoleChanged)
            .Reverse()
            .ToArray();
        Assert.Collection(
            roleEvents,
            item =>
            {
                Assert.Equal(TelegramUserRole.Consumer, item.OldRole);
                Assert.Equal(TelegramUserRole.Installer, item.NewRole);
            },
            item =>
            {
                Assert.Equal(TelegramUserRole.Installer, item.OldRole);
                Assert.Equal(TelegramUserRole.Consumer, item.NewRole);
            },
            item =>
            {
                Assert.Equal(TelegramUserRole.Consumer, item.OldRole);
                Assert.Equal(TelegramUserRole.Installer, item.NewRole);
            },
            item =>
            {
                Assert.Equal(TelegramUserRole.Installer, item.OldRole);
                Assert.Equal(TelegramUserRole.Engineer, item.NewRole);
            });
        var auditText = await ownerHarness.AuditService.FormatLatestAsync();
        Assert.Contains("Монтажник", auditText, StringComparison.Ordinal);
        Assert.Contains("Сервис-инженер", auditText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCanPromoteConsumerToEngineerButCannotCreateOrManageAdminsOrOwner()
    {
        var harness = await CreateHarnessAsync();

        await harness.Service.HandleCallbackAsync(
            Callback($"au:r:{harness.Consumer.Id}:e", harness.Admin));
        var createAdmin = await harness.Service.HandleCallbackAsync(
            Callback($"au:r:{harness.OtherConsumer.Id}:a", harness.Admin));
        var manageOwner = await harness.Service.HandleCallbackAsync(
            Callback($"au:b:{harness.Owner.Id}", harness.Admin));
        var manageAdmin = await harness.Service.HandleCallbackAsync(
            Callback($"au:d:{harness.OtherAdmin.Id}", harness.Admin));

        Assert.Equal(
            TelegramUserRole.Engineer,
            (await harness.Users.GetByIdAsync(harness.Consumer.Id))?.Role);
        Assert.Equal("Нет доступа", createAdmin.CallbackAnswerText);
        Assert.Contains("Admin", createAdmin.Text, StringComparison.Ordinal);
        Assert.Equal("Owner защищён от этого действия.", manageOwner.CallbackAnswerText);
        Assert.Equal("Нет доступа", manageAdmin.CallbackAnswerText);
        Assert.False((await harness.Users.GetByIdAsync(harness.Owner.Id))?.IsBlocked);
        Assert.True((await harness.Users.GetByIdAsync(harness.OtherAdmin.Id))?.IsEnabled);
    }

    [Theory]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Installer)]
    [InlineData(TelegramUserRole.Consumer)]
    public async Task EngineerAndConsumerCannotManageUsers(TelegramUserRole role)
    {
        var harness = await CreateHarnessAsync();
        var actor = role switch
        {
            TelegramUserRole.Engineer => harness.Engineer,
            TelegramUserRole.Installer => harness.Installer,
            _ => harness.Consumer
        };

        var result = await harness.Service.HandleCallbackAsync(
            Callback($"au:b:{harness.OtherConsumer.Id}", actor));

        Assert.Equal("Нет доступа", result.CallbackAnswerText);
        Assert.False((await harness.Users.GetByIdAsync(harness.OtherConsumer.Id))?.IsBlocked);
    }

    [Fact]
    public async Task BlockUnblockDisableAndEnableMutateUserAndRefreshCard()
    {
        var harness = await CreateHarnessAsync();

        await harness.Service.HandleCallbackAsync(Callback($"au:b:{harness.Consumer.Id}", harness.Owner));
        Assert.True((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsBlocked);

        await harness.Service.HandleCallbackAsync(Callback($"au:u:{harness.Consumer.Id}", harness.Owner));
        Assert.False((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsBlocked);

        await harness.Service.HandleCallbackAsync(Callback($"au:d:{harness.Consumer.Id}", harness.Owner));
        Assert.False((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsEnabled);

        await harness.Service.HandleCallbackAsync(Callback($"au:en:{harness.Consumer.Id}", harness.Owner));
        Assert.True((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsEnabled);
        Assert.Equal(4, harness.Outbound.Edits.Count);
    }

    [Fact]
    public async Task SelfAndOwnerDestructiveActionsAreDenied()
    {
        var harness = await CreateHarnessAsync();

        var selfBlock = await harness.Service.HandleCallbackAsync(
            Callback($"au:b:{harness.Owner.Id}", harness.Owner));
        var selfDisable = await harness.Service.HandleCallbackAsync(
            Callback($"au:d:{harness.Admin.Id}", harness.Admin));
        var selfDemote = await harness.Service.HandleCallbackAsync(
            Callback($"au:r:{harness.Admin.Id}:c", harness.Admin));
        var ownerDemote = await harness.Service.HandleCallbackAsync(
            Callback($"au:r:{harness.Owner.Id}:c", harness.OtherAdmin));

        Assert.Contains("собственный доступ", selfBlock.Text, StringComparison.Ordinal);
        Assert.Contains("собственный доступ", selfDisable.Text, StringComparison.Ordinal);
        Assert.Contains("собственный доступ", selfDemote.Text, StringComparison.Ordinal);
        Assert.Contains("Owner защищён", ownerDemote.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramUserRole.Owner, (await harness.Users.GetByIdAsync(harness.Owner.Id))?.Role);
        Assert.Equal(TelegramUserRole.Admin, (await harness.Users.GetByIdAsync(harness.Admin.Id))?.Role);
    }

    [Fact]
    public async Task AdminCallbackIsAnsweredAndDoesNotSendExtraMessageWhenEditSucceeds()
    {
        var harness = await CreateHarnessAsync();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(
            new EquipmentDiagnosticTelegramWebhookOptions
            {
                IsEnabled = true,
                WebhookSecret = "test_webhook_secret",
                BotToken = "test-token"
            },
            new EquipmentDiagnosticTelegramWebhookSecurityPolicy(),
            harness.Adapter,
            harness.Outbound);
        var callback = new TelegramWebhookUpdateDto(
            12,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                "callback-admin",
                new TelegramWebhookUserDto(
                    harness.Owner.TelegramUserId!.Value,
                    harness.Owner.Username,
                    harness.Owner.FirstName,
                    harness.Owner.LastName),
                new TelegramWebhookMessageDto(
                    77,
                    Text: null,
                    new TelegramWebhookChatDto(harness.Owner.TelegramChatId, harness.Owner.Username, "private"),
                    From: null,
                    Date: null),
                $"au:r:{harness.Consumer.Id}:e"));

        var result = await handler.HandleAsync(callback, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, harness.Outbound.AnswerCount);
        Assert.Equal("Роль обновлена", harness.Outbound.LastAnswerText);
        Assert.Single(harness.Outbound.Edits);
        Assert.Empty(harness.Outbound.Messages);
    }

    [Theory]
    [InlineData("au:")]
    [InlineData("au:bad")]
    [InlineData("au:v:nope")]
    [InlineData("au:r:1:x")]
    public async Task InvalidAdminCallbackIsSafeAndDoesNotExposeSensitiveValues(string data)
    {
        var harness = await CreateHarnessAsync();

        var result = await harness.Service.HandleCallbackAsync(Callback(data, harness.Owner));

        Assert.Equal("Ошибка действия", result.CallbackAnswerText);
        Assert.True(result.SuppressOutbound);
        Assert.Empty(harness.Outbound.Edits);
        Assert.DoesNotContain(FullPhone, result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("test-token", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidAdminCallbackThroughWebhookIsStillAnswered()
    {
        var harness = await CreateHarnessAsync();
        var handler = new EquipmentDiagnosticTelegramWebhookHandler(
            new EquipmentDiagnosticTelegramWebhookOptions
            {
                IsEnabled = true,
                WebhookSecret = "test_webhook_secret",
                BotToken = "test-token"
            },
            new EquipmentDiagnosticTelegramWebhookSecurityPolicy(),
            harness.Adapter,
            harness.Outbound);
        var callback = new TelegramWebhookUpdateDto(
            13,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                "callback-invalid-admin",
                new TelegramWebhookUserDto(harness.Owner.TelegramUserId!.Value, harness.Owner.Username),
                new TelegramWebhookMessageDto(
                    77,
                    Text: null,
                    new TelegramWebhookChatDto(harness.Owner.TelegramChatId, harness.Owner.Username, "private"),
                    From: null,
                    Date: null),
                "au:r:nope:a"));

        var result = await handler.HandleAsync(callback, "test_webhook_secret");

        Assert.Equal(EquipmentDiagnosticTelegramWebhookStatus.Processed, result.Status);
        Assert.Equal(1, harness.Outbound.AnswerCount);
        Assert.Equal("Ошибка действия", harness.Outbound.LastAnswerText);
        Assert.Empty(harness.Outbound.Edits);
        Assert.Empty(harness.Outbound.Messages);
    }

    [Fact]
    public async Task EditFailureFallsBackToSafeCurrentCard()
    {
        var harness = await CreateHarnessAsync();
        harness.Outbound.EditSucceeds = false;

        await harness.Service.HandleCallbackAsync(
            Callback($"au:v:{harness.Consumer.Id}", harness.Owner));

        var fallback = Assert.Single(harness.Outbound.Messages);
        Assert.Equal(harness.Owner.TelegramChatId, fallback.ChatId);
        Assert.DoesNotContain(FullPhone, fallback.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuccessfulMutationsAppendDedicatedUserAuditEvents()
    {
        var harness = await CreateHarnessAsync();

        await harness.Service.HandleCallbackAsync(Callback($"au:r:{harness.Consumer.Id}:e", harness.Owner));
        await harness.Service.HandleCallbackAsync(Callback($"au:b:{harness.Consumer.Id}", harness.Owner));
        await harness.Service.HandleCallbackAsync(Callback($"au:u:{harness.Consumer.Id}", harness.Owner));
        await harness.Service.HandleCallbackAsync(Callback($"au:d:{harness.Consumer.Id}", harness.Owner));
        await harness.Service.HandleCallbackAsync(Callback($"au:en:{harness.Consumer.Id}", harness.Owner));

        var events = await harness.AuditStore.GetLatestAsync(10);

        Assert.Equal(
            [
                TelegramUserAuditEventType.UserEnabled,
                TelegramUserAuditEventType.UserDisabled,
                TelegramUserAuditEventType.UserUnblocked,
                TelegramUserAuditEventType.UserBlocked,
                TelegramUserAuditEventType.RoleChanged
            ],
            events.Select(item => item.EventType).ToArray());
        var role = events.Single(item => item.EventType == TelegramUserAuditEventType.RoleChanged);
        Assert.Equal(harness.Owner.Id, role.ActorTelegramUserId);
        Assert.Equal(harness.Consumer.Id, role.TargetTelegramUserId);
        Assert.Equal(TelegramUserRole.Consumer, role.OldRole);
        Assert.Equal(TelegramUserRole.Engineer, role.NewRole);
        Assert.True(role.IsSuccessful);
    }

    [Fact]
    public async Task DeniedMutationsAppendSafeReasonCodes()
    {
        var harness = await CreateHarnessAsync();

        await harness.Service.HandleCallbackAsync(Callback($"au:b:{harness.Owner.Id}", harness.Admin));
        await harness.Service.HandleCallbackAsync(Callback($"au:d:{harness.Admin.Id}", harness.Admin));
        await harness.Service.HandleCallbackAsync(Callback($"au:r:{harness.OtherConsumer.Id}:a", harness.Admin));
        await harness.Service.HandleCallbackAsync(Callback("au:r:999999:x", harness.Owner));
        await harness.Service.HandleCallbackAsync(Callback("au:b:999999", harness.Owner));
        await harness.Service.HandleCallbackAsync(Callback("au:unsupported:999999", harness.Owner));
        await harness.Users.SetEnabledAsync(harness.OtherAdmin.TelegramChatId, false);
        await harness.Service.HandleCallbackAsync(Callback($"au:b:{harness.OtherConsumer.Id}", harness.OtherAdmin));

        var events = await harness.AuditStore.GetLatestAsync(10);

        Assert.All(events, item =>
        {
            Assert.Equal(TelegramUserAuditEventType.UserActionDenied, item.EventType);
            Assert.False(item.IsSuccessful);
            Assert.DoesNotContain(FullPhone, item.MetadataJson ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain(harness.Owner.TelegramChatId.ToString(), item.MetadataJson ?? string.Empty, StringComparison.Ordinal);
        });
        Assert.Contains(events, item => item.MetadataJson!.Contains("owner_protected", StringComparison.Ordinal));
        Assert.Contains(events, item => item.MetadataJson!.Contains("self_action_denied", StringComparison.Ordinal));
        Assert.Contains(events, item => item.MetadataJson!.Contains("insufficient_permissions", StringComparison.Ordinal));
        Assert.Contains(events, item => item.MetadataJson!.Contains("invalid_role", StringComparison.Ordinal));
        Assert.Contains(events, item => item.MetadataJson!.Contains("target_not_found", StringComparison.Ordinal));
        Assert.Contains(events, item => item.MetadataJson!.Contains("unsupported_action", StringComparison.Ordinal));
        Assert.Contains(events, item => item.MetadataJson!.Contains("disabled_or_blocked_actor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuditMetadataDropsUnapprovedAndSensitiveFields()
    {
        var harness = await CreateHarnessAsync();
        await harness.AuditService.AppendSafeAsync(
            new TelegramUserAuditEventCreate(
                TelegramUserAuditEventType.UserActionDenied,
                harness.Owner.Id,
                harness.Consumer.Id,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                FullPhone,
                $$"""{"action":"block","reason":"owner_protected","phone":"{{FullPhone}}","token":"test-token","callback":"au:b:1"}""",
                FixedNowUtc));

        var auditEvent = Assert.Single(await harness.AuditStore.GetLatestAsync(10));

        Assert.Equal("Telegram user management action denied.", auditEvent.Message);
        Assert.Equal("""{"action":"block","reason":"owner_protected"}""", auditEvent.MetadataJson);
        Assert.DoesNotContain(FullPhone, auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("test-token", auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("callback", auditEvent.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminAuditShowsLatestSafeEventsAndIsDeniedForConsumer()
    {
        var harness = await CreateHarnessAsync();
        await harness.Service.HandleCallbackAsync(Callback($"au:b:{harness.Consumer.Id}", harness.Owner));

        var owner = await harness.Adapter.HandleAsync(Command("/admin_audit", harness.Owner));
        var admin = await harness.Adapter.HandleAsync(Command("/admin_audit", harness.Admin));
        var consumer = await harness.Adapter.HandleAsync(Command("/admin_audit", harness.OtherConsumer));

        Assert.Contains("Аудит управления пользователями", owner.Text, StringComparison.Ordinal);
        Assert.Contains("@owner", owner.Text, StringComparison.Ordinal);
        Assert.Contains("@consumer", owner.Text, StringComparison.Ordinal);
        Assert.Contains("заблокирован", owner.Text, StringComparison.Ordinal);
        Assert.Equal(owner.Text, admin.Text);
        Assert.Equal("Команда недоступна.", consumer.Text);
        Assert.DoesNotContain(FullPhone, owner.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Consumer.TelegramChatId.ToString(), owner.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Consumer.TelegramUserId!.Value.ToString(), owner.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditWriteFailureDoesNotRollbackUserMutation()
    {
        var harness = await CreateHarnessAsync();
        var audit = new TelegramUserAuditEventService(
            new ThrowingAuditStore(),
            harness.Users,
            new TelegramDisplayTimeFormatter(
                new EquipmentDiagnosticTelegramOptions { DisplayTimeZone = "Asia/Tashkent" },
                new FixedTimeProvider()));
        var service = new TelegramAdminUserManagementService(
            harness.Users,
            harness.Outbound,
            new TelegramDisplayTimeFormatter(
                new EquipmentDiagnosticTelegramOptions { DisplayTimeZone = "Asia/Tashkent" },
                new FixedTimeProvider()),
            audit);

        var result = await service.HandleCallbackAsync(
            Callback($"au:b:{harness.Consumer.Id}", harness.Owner));

        Assert.Equal("Пользователь заблокирован", result.CallbackAnswerText);
        Assert.True((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsBlocked);
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var options = new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            BootstrapOwnerChatId = 100,
            DisplayTimeZone = "Asia/Tashkent",
            MaxMessageLength = 1200
        };
        var users = new InMemoryTelegramUserStore();
        var owner = await users.EnsureBootstrapOwnerAsync(UserUpdate(100, 1000, "owner", "Owner"));
        var admin = await CreateUserAsync(users, 200, 2000, "admin", "Admin", TelegramUserRole.Admin);
        var otherAdmin = await CreateUserAsync(users, 201, 2001, "otheradmin", "Other Admin", TelegramUserRole.Admin);
        var engineer = await CreateUserAsync(users, 300, 3000, "engineer", "Engineer", TelegramUserRole.Engineer);
        var installer = await CreateUserAsync(users, 301, 3001, "installer", "Installer", TelegramUserRole.Installer);
        var consumer = await CreateUserAsync(users, 400, 4000, "consumer", "Consumer", TelegramUserRole.Consumer);
        var otherConsumer = await CreateUserAsync(users, 401, 4001, "otherconsumer", "Other Consumer", TelegramUserRole.Consumer);
        await users.SavePhoneAsync(consumer.TelegramChatId, FullPhone, false, TelegramUserPhoneNumberSource.Manual, FixedNowUtc);
        consumer = (await users.GetByIdAsync(consumer.Id))!;

        var outbound = new FakeOutbound();
        var formatter = new TelegramDisplayTimeFormatter(options, new FixedTimeProvider());
        var auditStore = new InMemoryTelegramUserAuditEventStore();
        var audit = new TelegramUserAuditEventService(auditStore, users, formatter);
        var service = new TelegramAdminUserManagementService(users, outbound, formatter, audit);
        var adapter = new EquipmentDiagnosticTelegramAdapter(
            new StaticFacade(),
            new EquipmentDiagnosticTelegramMessageParser(),
            new EquipmentDiagnosticTelegramResponseFormatter(),
            options,
            new TelegramUserAccessService(users, options),
            users,
            adminUserManagementService: service);
        return new Harness(
            users,
            auditStore,
            audit,
            outbound,
            service,
            adapter,
            owner,
            admin,
            otherAdmin,
            engineer,
            installer,
            consumer,
            otherConsumer);
    }

    private static async Task<TelegramUserSnapshot> CreateUserAsync(
        InMemoryTelegramUserStore users,
        long chatId,
        long telegramUserId,
        string username,
        string firstName,
        TelegramUserRole role)
    {
        var user = await users.GetOrCreateConsumerAsync(UserUpdate(chatId, telegramUserId, username, firstName));
        await users.SetRoleAsync(chatId, role);
        return (await users.GetByIdAsync(user.Id))!;
    }

    private static EquipmentDiagnosticTelegramUpdate UserUpdate(
        long chatId,
        long telegramUserId,
        string username,
        string firstName) =>
        new(
            1,
            chatId,
            username,
            "/start",
            MessageId: 1,
            ReceivedAt: FixedNowUtc,
            UserId: telegramUserId,
            FirstName: firstName,
            ChatType: "private");

    private static EquipmentDiagnosticTelegramUpdate Command(
        string text,
        TelegramUserSnapshot actor) =>
        new(
            2,
            actor.TelegramChatId,
            actor.Username,
            text,
            MessageId: 20,
            ReceivedAt: FixedNowUtc,
            UserId: actor.TelegramUserId,
            FirstName: actor.FirstName,
            LastName: actor.LastName,
            ChatType: "private");

    private static EquipmentDiagnosticTelegramUpdate Callback(
        string data,
        TelegramUserSnapshot actor) =>
        new(
            3,
            actor.TelegramChatId,
            actor.Username,
            Text: null,
            MessageId: 77,
            ReceivedAt: FixedNowUtc,
            UserId: actor.TelegramUserId,
            FirstName: actor.FirstName,
            LastName: actor.LastName,
            ChatType: "private",
            CallbackQueryId: "callback-id",
            CallbackData: data);

    private static IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton> Buttons(
        EquipmentDiagnosticTelegramResponse response) =>
        Buttons(response.OutboundMessages.Single().ReplyMarkup);

    private static IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton> Buttons(
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup) =>
        replyMarkup?.InlineKeyboard?.SelectMany(row => row).ToArray() ?? [];

    private sealed record Harness(
        InMemoryTelegramUserStore Users,
        InMemoryTelegramUserAuditEventStore AuditStore,
        TelegramUserAuditEventService AuditService,
        FakeOutbound Outbound,
        TelegramAdminUserManagementService Service,
        EquipmentDiagnosticTelegramAdapter Adapter,
        TelegramUserSnapshot Owner,
        TelegramUserSnapshot Admin,
        TelegramUserSnapshot OtherAdmin,
        TelegramUserSnapshot Engineer,
        TelegramUserSnapshot Installer,
        TelegramUserSnapshot Consumer,
        TelegramUserSnapshot OtherConsumer);

    private sealed class ThrowingAuditStore : ITelegramUserAuditEventStore
    {
        public Task<TelegramUserAuditEventSnapshot> AppendAsync(
            TelegramUserAuditEventCreate request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("audit unavailable");

        public Task<IReadOnlyList<TelegramUserAuditEventSnapshot>> GetLatestAsync(
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelegramUserAuditEventSnapshot>>([]);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => FixedNowUtc;
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public List<(long ChatId, string Text)> Messages { get; } = [];
        public List<(long ChatId, long MessageId, string Text)> Edits { get; } = [];
        public bool EditSucceeds { get; set; } = true;
        public int AnswerCount { get; private set; }
        public string? LastAnswerText { get; private set; }

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((chatId, text));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent.", 90));
        }

        public Task<EquipmentDiagnosticTelegramOutboundResult> EditMessageTextAsync(
            long chatId,
            long messageId,
            string text,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Edits.Add((chatId, messageId, text));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(
                EditSucceeds,
                EditSucceeds ? "Edited." : "Failed.",
                EditSucceeds ? messageId : null));
        }

        public Task<EquipmentDiagnosticTelegramOutboundResult> AnswerCallbackQueryAsync(
            string callbackQueryId,
            string? text = null,
            bool showAlert = false,
            CancellationToken cancellationToken = default)
        {
            AnswerCount++;
            LastAnswerText = text;
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Answered."));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Synced."));
    }

    private sealed class StaticFacade : IEquipmentDiagnosticBotFacade
    {
        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticBotResponse(
                EquipmentDiagnosticBotResponseStatus.Answer,
                "Gree H5",
                "Technical answer.",
                "Gree",
                "H5",
                null,
                new EquipmentDiagnosticBotObservedCodeContext("H5", "H5", null),
                null,
                null,
                null,
                new EquipmentDiagnosticBotSafetyCard("Qualified technician required.", []),
                true,
                DiagnosticConfidence.Medium,
                false,
                true,
                [],
                [],
                null));
    }
}
