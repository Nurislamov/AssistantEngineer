using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramUserOverviewTests
{
    private const string FullPhone = "+998901234567";
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OwnerLibraryMenuShowsUsersButtonButOtherRolesDoNot()
    {
        var harness = await CreateHarnessAsync();

        var owner = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Owner));
        var admin = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Admin));
        var engineer = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Engineer));
        var installer = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Installer));
        var consumer = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Consumer));

        Assert.Contains(InlineButtons(owner), button => button.Text == TelegramUserOverviewService.UsersButton);
        Assert.DoesNotContain(InlineButtons(admin), button => button.Text == TelegramUserOverviewService.UsersButton);
        Assert.DoesNotContain(InlineButtons(engineer), button => button.Text == TelegramUserOverviewService.UsersButton);
        Assert.DoesNotContain(InlineButtons(installer), button => button.Text == TelegramUserOverviewService.UsersButton);
        Assert.DoesNotContain(InlineButtons(consumer), button => button.Text == TelegramUserOverviewService.UsersButton);
    }

    [Fact]
    public async Task OwnerOverviewCountsRolesAndBroadcastReachability()
    {
        var harness = await CreateHarnessAsync();

        var response = await harness.Adapter.HandleAsync(Callback("usr:stats", harness.Owner));

        Assert.Contains("👥 Пользователи", response.Text, StringComparison.Ordinal);
        Assert.Contains("Всего пользователей: 7", response.Text, StringComparison.Ordinal);
        Assert.Contains("Активные: 5", response.Text, StringComparison.Ordinal);
        Assert.Contains("Доступны для будущей рассылки: 4", response.Text, StringComparison.Ordinal);
        Assert.Contains("Недоступны для личных сообщений: 3", response.Text, StringComparison.Ordinal);
        Assert.Contains("Owner: 1", response.Text, StringComparison.Ordinal);
        Assert.Contains("Admin: 1", response.Text, StringComparison.Ordinal);
        Assert.Contains("Engineer: 2", response.Text, StringComparison.Ordinal);
        Assert.Contains("Installer: 1", response.Text, StringComparison.Ordinal);
        Assert.Contains("Consumer: 2", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OwnerRoleListPaginatesAndDoesNotExposePhoneOrPersonalCallbackData()
    {
        var harness = await CreateHarnessAsync();
        for (var index = 0; index < 12; index++)
        {
            await CreateUserAsync(harness.Users, 1000 + index, 10000 + index, $"engineer{index}", $"Engineer {index}", TelegramUserRole.Engineer);
        }

        var pageOne = await harness.Adapter.HandleAsync(Callback("usr:r:eng:0", harness.Owner));
        var nextCallback = Assert.Single(InlineButtons(pageOne), button => button.Text == "➡️ Далее").CallbackData;
        var pageTwo = await harness.Adapter.HandleAsync(Callback(nextCallback, harness.Owner));

        Assert.Contains("👥 Пользователи — Engineer", pageOne.Text, StringComparison.Ordinal);
        Assert.Contains("Всего: 14", pageOne.Text, StringComparison.Ordinal);
        Assert.Contains("Страница 1/2", pageOne.Text, StringComparison.Ordinal);
        Assert.Contains("@engineer", pageOne.Text, StringComparison.Ordinal);
        Assert.Contains("Статус: Активен", pageOne.Text, StringComparison.Ordinal);
        Assert.Contains("Рассылка: да", pageOne.Text, StringComparison.Ordinal);
        Assert.Contains("Страница 2/2", pageTwo.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, pageOne.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, pageTwo.Text, StringComparison.Ordinal);
        Assert.All(InlineButtons(pageOne).Concat(InlineButtons(pageTwo)), button =>
        {
            Assert.True(Encoding.UTF8.GetByteCount(button.CallbackData) <= 64, button.CallbackData);
            Assert.DoesNotContain("engineer", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("1000", button.CallbackData, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task NonOwnerCallbacksAreDeniedWithoutUserDataLeak()
    {
        var harness = await CreateHarnessAsync();

        var admin = await harness.Adapter.HandleAsync(Callback("usr:stats", harness.Admin));
        var consumer = await harness.Adapter.HandleAsync(Callback("usr:r:eng:0", harness.Consumer));
        var stale = await harness.Adapter.HandleAsync(Callback("usr:r:bad:0", harness.Owner));

        Assert.Equal("Раздел пользователей доступен только владельцу.", admin.Text);
        Assert.Equal("Раздел пользователей доступен только владельцу.", consumer.Text);
        Assert.Equal("Список пользователей устарел.", stale.Text);
        Assert.DoesNotContain("@engineer", admin.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("@engineer", consumer.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, admin.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OwnerCanOpenUserCardWithoutPhoneDisclosure()
    {
        var harness = await CreateHarnessAsync();

        var list = await harness.Adapter.HandleAsync(Callback("usr:r:cons:0", harness.Owner));
        var view = Assert.Single(
            InlineButtons(list),
            button => button.CallbackData == $"usr:view:{harness.Consumer.Id}");
        var card = await harness.Adapter.HandleAsync(Callback(view.CallbackData, harness.Owner));

        Assert.Contains("👤 Пользователь", card.Text, StringComparison.Ordinal);
        Assert.Contains("Имя: Consumer", card.Text, StringComparison.Ordinal);
        Assert.Contains("Username: @consumer", card.Text, StringComparison.Ordinal);
        Assert.Contains("TelegramId: 5000", card.Text, StringComparison.Ordinal);
        Assert.Contains("Текущая роль: Consumer", card.Text, StringComparison.Ordinal);
        Assert.Contains("Статус: Активен", card.Text, StringComparison.Ordinal);
        Assert.Contains("Личный чат: есть", card.Text, StringComparison.Ordinal);
        Assert.Contains("Доступен для рассылки: да", card.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(FullPhone, card.Text, StringComparison.Ordinal);
        Assert.Contains(InlineButtons(card), button => button.CallbackData == $"usr:role:{harness.Consumer.Id}");
        Assert.Contains(InlineButtons(card), button => button.CallbackData == $"usr:block:{harness.Consumer.Id}");
    }

    [Fact]
    public async Task OwnerChangesRoleOnlyAfterConfirmationAndCountsFollowNewRole()
    {
        var harness = await CreateHarnessAsync();

        var picker = await harness.Adapter.HandleAsync(Callback($"usr:role:{harness.Consumer.Id}", harness.Owner));
        var chooseEngineer = Assert.Single(
            InlineButtons(picker),
            button => button.CallbackData == $"usr:set:{harness.Consumer.Id}:eng");
        var confirmation = await harness.Adapter.HandleAsync(Callback(chooseEngineer.CallbackData, harness.Owner));

        Assert.Contains("Назначить роль Engineer", confirmation.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramUserRole.Consumer, (await harness.Users.GetByIdAsync(harness.Consumer.Id))?.Role);

        var confirm = Assert.Single(
            InlineButtons(confirmation),
            button => button.CallbackData == $"usr:confirm:set:{harness.Consumer.Id}:eng");
        var updatedCard = await harness.Adapter.HandleAsync(Callback(confirm.CallbackData, harness.Owner));
        var overview = await harness.Adapter.HandleAsync(Callback("usr:stats", harness.Owner));

        Assert.Equal(TelegramUserRole.Engineer, (await harness.Users.GetByIdAsync(harness.Consumer.Id))?.Role);
        Assert.Contains("Текущая роль: Engineer", updatedCard.Text, StringComparison.Ordinal);
        Assert.Contains("Engineer: 3", overview.Text, StringComparison.Ordinal);
        Assert.Contains("Consumer: 1", overview.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OwnerBlocksAndUnblocksUserWithEarlyDenialAndBroadcastExclusion()
    {
        var harness = await CreateHarnessAsync();
        var before = await harness.Users.GetUserOverviewAsync();

        var prompt = await harness.Adapter.HandleAsync(
            Callback($"usr:block:{harness.Consumer.Id}", harness.Owner));
        Assert.Contains("Подтвердить блокировку", prompt.Text, StringComparison.Ordinal);
        Assert.False((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsBlocked);

        var blockedCard = await harness.Adapter.HandleAsync(
            Callback($"usr:confirm:block:{harness.Consumer.Id}", harness.Owner));
        var afterBlock = await harness.Users.GetUserOverviewAsync();
        Assert.Contains("Статус: Заблокирован", blockedCard.Text, StringComparison.Ordinal);
        Assert.True((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsBlocked);
        Assert.Equal(before.BroadcastReachableCount - 1, afterBlock.BroadcastReachableCount);

        foreach (var text in new[]
        {
            "/start",
            "Gree H5",
            TelegramManualLibraryService.LibraryButton,
            TelegramDiagnosticConversationService.ServiceRequestButton
        })
        {
            var denied = await harness.Adapter.HandleAsync(Command(text, harness.Consumer));
            Assert.Equal(EquipmentDiagnosticTelegramAdapter.BlockedUserMessage, denied.Text);
            Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, denied.ResponseKind);
        }

        var unblockPrompt = await harness.Adapter.HandleAsync(
            Callback($"usr:unblock:{harness.Consumer.Id}", harness.Owner));
        Assert.Contains("Подтвердить разблокировку", unblockPrompt.Text, StringComparison.Ordinal);
        var activeCard = await harness.Adapter.HandleAsync(
            Callback($"usr:confirm:unblock:{harness.Consumer.Id}", harness.Owner));
        var afterUnblock = await harness.Users.GetUserOverviewAsync();

        Assert.Contains("Статус: Активен", activeCard.Text, StringComparison.Ordinal);
        Assert.False((await harness.Users.GetByIdAsync(harness.Consumer.Id))?.IsBlocked);
        Assert.Equal(before.BroadcastReachableCount, afterUnblock.BroadcastReachableCount);
        var start = await harness.Adapter.HandleAsync(Command("/start", harness.Consumer));
        Assert.NotEqual(EquipmentDiagnosticTelegramAdapter.BlockedUserMessage, start.Text);
    }

    [Fact]
    public async Task OwnerSelfAndLastOwnerProtectionsRejectDirectCallbacks()
    {
        var harness = await CreateHarnessAsync();

        var selfBlock = await harness.Adapter.HandleAsync(
            Callback($"usr:confirm:block:{harness.Owner.Id}", harness.Owner));
        var lastOwnerDemotion = await harness.Adapter.HandleAsync(
            Callback($"usr:confirm:set:{harness.Owner.Id}:cons", harness.Owner));

        Assert.Equal("Нельзя изменить собственный уровень или заблокировать себя.", selfBlock.Text);
        Assert.Equal("Нельзя понизить или заблокировать последнего Owner.", lastOwnerDemotion.Text);
        var owner = await harness.Users.GetByIdAsync(harness.Owner.Id);
        Assert.Equal(TelegramUserRole.Owner, owner?.Role);
        Assert.False(owner?.IsBlocked);
    }

    [Fact]
    public async Task NonOwnerManagementCallbackIsSafeAndCallbacksContainNoPersonalData()
    {
        var harness = await CreateHarnessAsync();

        var denied = await harness.Adapter.HandleAsync(
            Callback($"usr:view:{harness.Consumer.Id}", harness.Admin));
        var deniedLegacy = await harness.Adapter.HandleAsync(
            Callback($"au:v:{harness.Consumer.Id}", harness.Admin));
        var ownerList = await harness.Adapter.HandleAsync(
            Callback("usr:r:cons:0", harness.Owner));

        Assert.Equal("Раздел пользователей доступен только владельцу.", denied.Text);
        Assert.Equal("Раздел пользователей доступен только владельцу.", deniedLegacy.Text);
        Assert.DoesNotContain("@consumer", denied.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("@consumer", deniedLegacy.Text, StringComparison.Ordinal);
        Assert.All(InlineButtons(ownerList), button =>
        {
            Assert.True(Encoding.UTF8.GetByteCount(button.CallbackData) <= 64, button.CallbackData);
            Assert.DoesNotContain("consumer", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(FullPhone, button.CallbackData, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task UserManagementServiceReturnsTypedSafetyResults()
    {
        var harness = await CreateHarnessAsync();
        var service = new TelegramUserManagementService(harness.Users);

        var invalid = await service.ChangeUserRoleAsync(
            harness.Consumer.Id,
            (TelegramUserRole)999,
            harness.Owner.Id);
        var missing = await service.BlockUserAsync(long.MaxValue, harness.Owner.Id);
        var nonOwner = await service.BlockUserAsync(
            harness.Consumer.Id,
            harness.Admin.Id);

        Assert.Equal(TelegramUserManagementStatus.InvalidRole, invalid.Status);
        Assert.Equal(TelegramUserManagementStatus.NotFound, missing.Status);
        Assert.Equal(TelegramUserManagementStatus.AccessDenied, nonOwner.Status);
    }

    [Fact]
    public async Task EmptyOverviewIsSafe()
    {
        var users = new InMemoryTelegramUserStore();
        var service = new TelegramUserOverviewService(users);

        var result = await service.BuildOverviewAsync();

        Assert.Contains("Всего пользователей: 0", result.Text, StringComparison.Ordinal);
        Assert.Contains("Owner: 0", result.Text, StringComparison.Ordinal);
        Assert.Contains("Consumer: 0", result.Text, StringComparison.Ordinal);
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var options = new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            BootstrapOwnerChatId = 100,
            MaxMessageLength = 1200
        };
        var users = new InMemoryTelegramUserStore();
        var owner = await users.EnsureBootstrapOwnerAsync(UserUpdate(100, 1000, "owner", "Owner"));
        var admin = await CreateUserAsync(users, 200, 2000, "admin", "Admin", TelegramUserRole.Admin);
        var engineer = await CreateUserAsync(users, 300, 3000, "engineer", "Engineer", TelegramUserRole.Engineer);
        var blockedEngineer = await CreateUserAsync(users, 301, 3001, "blockedengineer", "Blocked Engineer", TelegramUserRole.Engineer);
        await users.SetBlockedAsync(blockedEngineer.TelegramChatId, true);
        var installer = await CreateUserAsync(users, -400, 4000, "installer", "Installer", TelegramUserRole.Installer);
        var consumer = await CreateUserAsync(users, 500, 5000, "consumer", "Consumer", TelegramUserRole.Consumer);
        var disabledConsumer = await CreateUserAsync(users, 501, 5001, "disabledconsumer", "Disabled Consumer", TelegramUserRole.Consumer);
        await users.SetEnabledAsync(disabledConsumer.TelegramChatId, false);
        await users.SavePhoneAsync(consumer.TelegramChatId, FullPhone, false, TelegramUserPhoneNumberSource.Manual, FixedNowUtc);
        consumer = (await users.GetByIdAsync(consumer.Id))!;

        var library = new TelegramManualLibraryService(
            options,
            new InMemoryTelegramDiagnosticCaseStore(),
            new EmptyLocalizationSource(),
            new EmptyManualRegistrySource(),
            new FileTelegramManualFileBindingStore(options),
            users,
            new InMemoryTelegramLibraryAccessStore(),
            new DisabledEquipmentDiagnosticTelegramOutboundClient());
        var userOverview = new TelegramUserOverviewService(users);
        var adapter = new EquipmentDiagnosticTelegramAdapter(
            new StaticFacade(),
            new EquipmentDiagnosticTelegramMessageParser(),
            new EquipmentDiagnosticTelegramResponseFormatter(),
            options,
            new TelegramUserAccessService(users, options, new InMemoryTelegramLibraryAccessStore()),
            users,
            userOverviewService: userOverview,
            manualLibraryService: library);

        return new Harness(users, adapter, owner, admin, engineer, installer, consumer);
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
            ChatType: chatId > 0 ? "private" : "group");

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
            ChatType: actor.TelegramChatId > 0 ? "private" : "group");

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
            ChatType: actor.TelegramChatId > 0 ? "private" : "group",
            CallbackQueryId: "callback-id",
            CallbackData: data);

    private static IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton> InlineButtons(
        EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages.Single().ReplyMarkup?.InlineKeyboard?.SelectMany(row => row).ToArray() ?? [];

    private sealed record Harness(
        InMemoryTelegramUserStore Users,
        EquipmentDiagnosticTelegramAdapter Adapter,
        TelegramUserSnapshot Owner,
        TelegramUserSnapshot Admin,
        TelegramUserSnapshot Engineer,
        TelegramUserSnapshot Installer,
        TelegramUserSnapshot Consumer);

    private sealed class StaticFacade : IEquipmentDiagnosticBotFacade
    {
        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("User overview tests must not call diagnostics.");
    }

    private sealed class EmptyLocalizationSource : IErrorKnowledgeLocalizationSource
    {
        public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => [];

        public ErrorKnowledgeLocalizationSelection? Select(
            EquipmentDiagnosticBotResponse response,
            string locale,
            ErrorKnowledgeAudience audience) =>
            null;
    }

    private sealed class EmptyManualRegistrySource : ITelegramManualRegistrySource
    {
        public IReadOnlyList<TelegramManualRegistryEntry> GetManuals() => [];
    }
}
