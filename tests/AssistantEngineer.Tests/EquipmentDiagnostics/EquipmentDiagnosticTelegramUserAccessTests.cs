using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
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
        Assert.Contains("Telegram users", users.Text, StringComparison.Ordinal);
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
    }

    [Fact]
    public async Task ConsumerHelpDoesNotListAdminCommandsAndConsumerResponseIsSimplified()
    {
        var store = new InMemoryTelegramUserStore();
        var adapter = CreateAdapter(store, Options());

        var help = await adapter.HandleAsync(Update("/start", chatId: 600));
        var diagnostic = await adapter.HandleAsync(Update("Gree H5", chatId: 600));

        Assert.DoesNotContain("/admin", help.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Safe next steps:", diagnostic.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Confidence:", diagnostic.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EngineerReceivesTechnicalResponse()
    {
        var store = new InMemoryTelegramUserStore();
        await store.AllowAsync(700, TelegramUserRole.Engineer);
        var adapter = CreateAdapter(store, Options());

        var diagnostic = await adapter.HandleAsync(Update("Gree H5", chatId: 700));

        Assert.Contains("Confidence:", diagnostic.Text, StringComparison.Ordinal);
        Assert.Contains("Safety:", diagnostic.Text, StringComparison.Ordinal);
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
        Assert.True(user?.HasPhoneNumber);
        Assert.Equal(expectedVerified, user?.PhoneNumberVerified);
    }

    private static EquipmentDiagnosticTelegramAdapter CreateAdapter(
        ITelegramUserStore store,
        EquipmentDiagnosticTelegramOptions options)
    {
        var access = new TelegramUserAccessService(store, options);
        return new(
            new StaticFacade(),
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
}
