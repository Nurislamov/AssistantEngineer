using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramUserAccessTests
{
    [Fact]
    public async Task BootstrapOwnerIsCreatedAndUpdatedAsActiveOwner()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options() with { BootstrapOwnerChatId = 100 });

        var response = await adapter.HandleAsync(Update("/me", chatId: 100));
        var user = await store.GetByChatIdAsync(100);

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.NotNull(user);
        Assert.Equal(TelegramUserRole.Owner, user.Role);
        Assert.True(user.IsEnabled);
        Assert.False(user.IsBlocked);
    }

    [Fact]
    public async Task UnknownUserIsAutoCreatedAsEnabledConsumer()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options());

        var response = await adapter.HandleAsync(Update("/me", chatId: 200));
        var user = await store.GetByChatIdAsync(200);

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.NotNull(user);
        Assert.Equal(TelegramUserRole.Consumer, user.Role);
        Assert.True(user.IsEnabled);
        Assert.False(user.IsBlocked);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task BlockedOrDisabledUserCannotUseDiagnostics(bool isEnabled, bool isBlocked)
    {
        var store = new InMemoryTelegramUserStore();
        await store.AllowAsync(300, TelegramUserRole.Consumer);
        await store.SetEnabledAsync(300, isEnabled);
        await store.SetBlockedAsync(300, isBlocked);
        var adapter = CreateAdapter(store, Options());

        var response = await adapter.HandleAsync(Update("Gree H5", chatId: 300));
        var user = await store.GetByChatIdAsync(300);

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Ignored, response.ResponseKind);
        Assert.NotNull(user?.LastAccessDeniedAt);
    }

    [Fact]
    public async Task OwnerCanUseAdminUsersAndAllowCommands()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options() with { BootstrapOwnerChatId = 400 });

        var allow = await adapter.HandleAsync(Update("/admin allow 401", chatId: 400));
        var users = await adapter.HandleAsync(Update("/admin users", chatId: 400));
        var allowed = await store.GetByChatIdAsync(401);

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, allow.ResponseKind);
        Assert.Equal(TelegramUserRole.Consumer, allowed?.Role);
        Assert.Contains("Пользователи Telegram", users.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminHelpReturnsAdminCommandsOnlyForOwnerOrAdmin()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options() with { BootstrapOwnerChatId = 410 });

        var owner = await adapter.HandleAsync(Update("/admin_help", chatId: 410));
        await adapter.HandleAsync(Update("/admin allow 411", chatId: 410));
        await adapter.HandleAsync(Update("/admin role 411 Admin", chatId: 410));
        var admin = await adapter.HandleAsync(Update("/admin_help", chatId: 411));
        await adapter.HandleAsync(Update("/admin allow 413", chatId: 410));
        await adapter.HandleAsync(Update("/admin role 413 Engineer", chatId: 410));
        var engineer = await adapter.HandleAsync(Update("/admin_help", chatId: 413));
        var consumer = await adapter.HandleAsync(Update("/admin_help", chatId: 412));

        Assert.Contains("/admin users", owner.Text, StringComparison.Ordinal);
        Assert.Contains("/admin role <chatId>", admin.Text, StringComparison.Ordinal);
        Assert.Contains("/admin_users", owner.Text, StringComparison.Ordinal);
        Assert.Contains("/admin_pending", admin.Text, StringComparison.Ordinal);
        Assert.Contains("/engineers", admin.Text, StringComparison.Ordinal);
        Assert.Contains("Команда недоступна", engineer.Text, StringComparison.Ordinal);
        Assert.Contains("Команда недоступна", consumer.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminRoleCommandChangesRoleAndEngineerCannotUseAdminCommands()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options() with { BootstrapOwnerChatId = 500 });

        await adapter.HandleAsync(Update("/admin allow 501", chatId: 500));
        var role = await adapter.HandleAsync(Update("/admin role 501 Engineer", chatId: 500));
        var engineerAdmin = await adapter.HandleAsync(Update("/admin users", chatId: 501));
        var user = await store.GetByChatIdAsync(501);

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, role.ResponseKind);
        Assert.Equal(TelegramUserRole.Engineer, user?.Role);
        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Unsupported, engineerAdmin.ResponseKind);
        Assert.Contains("Команда недоступна", engineerAdmin.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConsumerHelpDoesNotListAdminCommandsAndConsumerResponseIsSimplified()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options());

        var help = await adapter.HandleAsync(Update("/help", chatId: 600));
        var diagnostic = await adapter.HandleAsync(Update("Gree H5", chatId: 600));

        Assert.Contains("Диагностика оборудования", help.Text, StringComparison.Ordinal);
        Assert.Contains("поделиться номером Telegram", help.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ввести другой номер", help.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin", help.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin_help", help.Text, StringComparison.Ordinal);
        Assert.Contains("Возможное значение", diagnostic.Text, StringComparison.Ordinal);
        Assert.Contains("Что можно сделать безопасно", diagnostic.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Confidence:", diagnostic.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Source:", diagnostic.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Response shortened", diagnostic.Text, StringComparison.Ordinal);
        Assert.NotNull(diagnostic.OutboundMessages.Single().ReplyMarkup);
    }

    [Fact]
    public async Task OwnerAndAdminHelpMentionAdminHelpWithoutListingParameterizedAdminCommands()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options() with { BootstrapOwnerChatId = 610 });

        await adapter.HandleAsync(Update("/admin allow 612", chatId: 610));
        await adapter.HandleAsync(Update("/admin role 612 Admin", chatId: 610));
        var ownerHelp = await adapter.HandleAsync(Update("/help", chatId: 610));
        var adminHelp = await adapter.HandleAsync(Update("/help", chatId: 612));

        Assert.Contains("/admin_help", ownerHelp.Text, StringComparison.Ordinal);
        Assert.Contains("/admin_help", adminHelp.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin users", ownerHelp.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin role", ownerHelp.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin users", adminHelp.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin role", adminHelp.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EngineerHelpDoesNotShowAdminCommands()
    {
        var store = new InMemoryTelegramUserStore();
        await store.AllowAsync(611, TelegramUserRole.Engineer);
        var adapter = CreateAdapter(store, Options());

        var help = await adapter.HandleAsync(Update("/help", chatId: 611));

        Assert.DoesNotContain("/admin_help", help.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin users", help.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EngineerReceivesTechnicalResponse()
    {
        var store = new InMemoryTelegramUserStore();
        await store.AllowAsync(700, TelegramUserRole.Engineer);
        var adapter = CreateAdapter(store, Options());

        var diagnostic = await adapter.HandleAsync(Update("Gree H5", chatId: 700));

        Assert.Contains("Уверенность:", diagnostic.Text, StringComparison.Ordinal);
        Assert.Contains("Безопасность:", diagnostic.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TechnicalResponseCanBeSplitIntoMultipleTelegramMessages()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(
            store,
            Options() with { BootstrapOwnerChatId = 710 },
            new LongTechnicalFacade());

        var response = await adapter.HandleAsync(Update("Gree H5", chatId: 710));

        Assert.True(response.OutboundMessages.Count > 1);
        Assert.All(response.OutboundMessages, message =>
            Assert.InRange(message.Text.Length, 1, 3500));
        Assert.StartsWith("Диагностика", response.OutboundMessages[0].Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(800, true)]
    [InlineData(801, false)]
    public async Task ContactMessageSavesPhoneAndVerifiesOnlyMatchingTelegramUser(long contactUserId, bool expectedVerified)
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options());

        var response = await adapter.HandleAsync(Update(
            text: null,
            chatId: 800,
            userId: 800,
            contactPhone: "+998901234567",
            contactUserId: contactUserId));
        var user = await store.GetByChatIdAsync(800);

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Спасибо, номер сохранен", response.Text, StringComparison.Ordinal);
        Assert.True(user?.HasPhoneNumber);
        Assert.Equal(expectedVerified, user?.PhoneNumberVerified);
        Assert.Equal(TelegramUserPhoneNumberSource.TelegramContact, user?.PhoneNumberSource);
        Assert.Contains("🔎 Новый код", response.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row).Select(button => button.Text));
    }

    [Fact]
    public async Task ConsumerWithSavedPhoneDoesNotGetRepeatedPhonePromptOrKeyboard()
    {
        var store = new InMemoryTelegramUserStore();
        await store.GetOrCreateConsumerAsync(Update("/start", chatId: 900));
        await store.SavePhoneAsync(900, "+998901234567", verified: true, TelegramUserPhoneNumberSource.TelegramContact, DateTimeOffset.UtcNow);
        var adapter = CreateAdapter(store, Options());

        var response = await adapter.HandleAsync(Update("Gree H5", chatId: 900));

        Assert.Contains("Ваш номер уже сохранен", response.Text, StringComparison.Ordinal);
        var buttons = response.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row).Select(button => button.Text).ToArray();
        Assert.Contains("🔎 Новый код", buttons);
        Assert.Contains(TelegramDiagnosticConversationService.ChangePhoneButton, buttons);
        Assert.DoesNotContain(TelegramDiagnosticConversationService.SharePhoneButton, buttons);
    }

    [Fact]
    public async Task ConsumerWithoutPhoneGetsTelegramAndManualPhoneButtons()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options());

        var response = await adapter.HandleAsync(Update("/start", chatId: 901));
        var buttons = response.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row).Select(button => button.Text).ToArray();

        Assert.Contains(TelegramDiagnosticConversationService.SharePhoneButton, buttons);
        Assert.Contains(TelegramDiagnosticConversationService.ManualPhoneButton, buttons);
    }

    [Fact]
    public async Task ConsumerMeShowsPhoneSavedAfterManualPhone()
    {
        var store = new InMemoryTelegramUserStore();
        await store.GetOrCreateConsumerAsync(Update("/start", chatId: 902));
        await store.SavePhoneAsync(902, "+998901234567", verified: false, TelegramUserPhoneNumberSource.Manual, DateTimeOffset.UtcNow);
        var adapter = CreateAdapter(store, Options());

        var response = await adapter.HandleAsync(Update("/me", chatId: 902));

        Assert.Contains("Телефон: сохранен", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("+998901234567", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminUsersShowsPhoneSourceWithoutRawPhoneNumber()
    {
        var store = new InMemoryTelegramUserStore();
        await store.GetOrCreateConsumerAsync(Update("/start", chatId: 904));
        await store.SavePhoneAsync(904, "+998901234567", verified: false, TelegramUserPhoneNumberSource.Manual, DateTimeOffset.UtcNow);
        var adapter = CreateAdapter(store, Options() with { BootstrapOwnerChatId = 903 });

        var response = await adapter.HandleAsync(Update("/admin users", chatId: 903));

        Assert.Contains("телефон=да(manual)", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("+998901234567", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("998901234567", response.Text, StringComparison.Ordinal);
    }

    private static EquipmentDiagnosticTelegramAdapter CreateAdapter(
        ITelegramUserStore store,
        EquipmentDiagnosticTelegramOptions options,
        IEquipmentDiagnosticBotFacade? facade = null)
    {
        var access = new TelegramUserAccessService(store, options);
        return new(
            facade ?? new StaticFacade(),
            new EquipmentDiagnosticTelegramMessageParser(),
            new EquipmentDiagnosticTelegramResponseFormatter(),
            options,
            access,
            store);
    }

    private static EquipmentDiagnosticTelegramOptions Options() => new()
    {
        IsEnabled = true,
        DefaultManufacturer = "Gree",
        MaxMessageLength = 1000
    };

    private static EquipmentDiagnosticTelegramUpdate Update(
        string? text,
        long chatId,
        long? userId = 11,
        string? contactPhone = null,
        long? contactUserId = null) =>
        new(
            UpdateId: 1,
            ChatId: chatId,
            Username: "operator",
            Text: text,
            UserId: userId,
            ContactPhoneNumber: contactPhone,
            ContactUserId: contactUserId);

    private sealed class StaticFacade : IEquipmentDiagnosticBotFacade
    {
        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticBotResponse(
                EquipmentDiagnosticBotResponseStatus.Answer,
                "Gree H5",
                "Possible compressor overload indication.",
                request.Manufacturer ?? "Gree",
                request.Code ?? "H5",
                null,
                new EquipmentDiagnosticBotObservedCodeContext(request.Code ?? "H5", request.Code ?? "H5", request.FreeText),
                new EquipmentDiagnosticBotAnswerCard(
                    "Gree H5",
                    "The unit reports a protected operating condition.",
                    "Verification required.",
                    [],
                    [],
                    [],
                    [],
                    []),
                null,
                new EquipmentDiagnosticBotSourceCard("Seed", "Seed", "Seed-only diagnostic.", null, null, null, null, null, []),
                new EquipmentDiagnosticBotSafetyCard("Qualified technician required.", ["Do not bypass protections."]),
                VerificationRequired: true,
                Confidence: DiagnosticConfidence.Medium,
                IsManualVerified: false,
                IsSeedKnowledge: true,
                OperatorNextSteps: ["Share the code with service."],
                Warnings: [],
                InternalDecisionTrace: null));
    }

    private sealed class LongTechnicalFacade : IEquipmentDiagnosticBotFacade
    {
        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default)
        {
            var longText = string.Join(
                Environment.NewLine,
                Enumerable.Range(1, 120).Select(index => $"Техническая строка {index}: проверка должна идти по документации и месту установки."));

            return Task.FromResult(new EquipmentDiagnosticBotResponse(
                EquipmentDiagnosticBotResponseStatus.Answer,
                "Gree H5",
                longText,
                request.Manufacturer ?? "Gree",
                request.Code ?? "H5",
                null,
                new EquipmentDiagnosticBotObservedCodeContext(request.Code ?? "H5", request.Code ?? "H5", request.FreeText),
                null,
                null,
                null,
                new EquipmentDiagnosticBotSafetyCard("Работы выполняет квалифицированный специалист.", []),
                VerificationRequired: true,
                Confidence: DiagnosticConfidence.Medium,
                IsManualVerified: false,
                IsSeedKnowledge: true,
                OperatorNextSteps: [],
                Warnings: [],
                InternalDecisionTrace: null));
        }
    }
}
