using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
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
        Assert.Contains("Личный чат: да", pageOne.Text, StringComparison.Ordinal);
        Assert.Contains("Для рассылки: да", pageOne.Text, StringComparison.Ordinal);
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
