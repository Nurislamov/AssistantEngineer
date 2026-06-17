using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramConversationStateMachineTests
{
    [Fact]
    public async Task ConsumerCodeOnlyWithMultipleBrandsAsksBrandWithRussianButtons()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);

        var response = await harness.Adapter.HandleAsync(Update("H5"));

        Assert.Contains("нескольких брендов", response.Text, StringComparison.OrdinalIgnoreCase);
        var buttons = ButtonTexts(response);
        Assert.Contains("Gree", buttons);
        Assert.Contains("Daikin", buttons);
        Assert.Contains(TelegramDiagnosticConversationService.NewCodeButton, buttons);
        Assert.DoesNotContain(TelegramDiagnosticConversationService.SharePhoneButton, buttons);
    }

    [Fact]
    public async Task SingleBrandCandidateSkipsBrandSelectionAndReturnsConsumerResult()
    {
        var harness = CreateHarness([Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)]);

        var response = await harness.Adapter.HandleAsync(Update("H5"));

        Assert.DoesNotContain("Выберите бренд", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
        Assert.Equal(1, harness.Facade.CallCount);
    }

    [Fact]
    public async Task MultipleDisplayContextsAskDisplayContextWithRussianButtons()
    {
        var harness = CreateHarness([]);
        var user = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start"));
        await harness.SessionStore.UpsertAsync(new TelegramConversationSessionUpsert(
            user.Id,
            TelegramConversationState.WaitingForDisplayContext,
            "C5",
            SelectedManufacturer: "Gree",
            SelectedEquipmentType: "Наружный блок",
            SelectedDisplayContext: null,
            JsonSerializer.Serialize(new[]
            {
                new TelegramDiagnosticCandidate(
                    "Gree",
                    "GMV",
                    null,
                    "C5",
                    EquipmentCategory.VrfOutdoorUnit,
                    "Наружный блок",
                    EquipmentDiagnosticBotEquipmentSide.Outdoor,
                    EquipmentDiagnosticBotDisplayContext.OduMainBoardLed),
                new TelegramDiagnosticCandidate(
                    "Gree",
                    "GMV",
                    null,
                    "C5",
                    EquipmentCategory.VrfOutdoorUnit,
                    "Наружный блок",
                    EquipmentDiagnosticBotEquipmentSide.Outdoor,
                    EquipmentDiagnosticBotDisplayContext.MobileAppOrGateway)
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            LastPromptMessageId: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30)));

        var response = await harness.Adapter.HandleAsync(Update("не то"));

        Assert.Contains("где отображается код", response.Text, StringComparison.OrdinalIgnoreCase);
        var buttons = ButtonTexts(response);
        Assert.Contains("Плата/LED", buttons);
        Assert.Contains("Приложение/шлюз", buttons);
        Assert.Contains(TelegramDiagnosticConversationService.NewCodeButton, buttons);
    }

    [Fact]
    public async Task SingleDisplayContextSkipsDisplayContextAndReturnsResult()
    {
        var harness = CreateHarness([Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)]);

        var response = await harness.Adapter.HandleAsync(Update("ошибка H5"));

        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("где отображается код", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NewCodeButtonClearsSessionAndPromptsForCode()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);
        await harness.Adapter.HandleAsync(Update("H5"));

        var response = await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.NewCodeButton));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Введите код ошибки", response.Text, StringComparison.Ordinal);
        Assert.Null(session);
    }

    [Fact]
    public async Task NewCommandClearsSessionAndPromptsForCode()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);
        await harness.Adapter.HandleAsync(Update("H5"));

        var response = await harness.Adapter.HandleAsync(Update("/new"));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Введите код ошибки", response.Text, StringComparison.Ordinal);
        Assert.Null(session);
    }

    [Fact]
    public async Task ManualPhoneButtonEntersWaitingForPhoneNumber()
    {
        var harness = CreateHarness([]);

        var response = await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.ManualPhoneButton));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Введите номер телефона", response.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramConversationState.WaitingForPhoneNumber, session?.State);
        var buttons = ButtonTexts(response);
        Assert.Contains(TelegramDiagnosticConversationService.CancelButton, buttons);
        Assert.Contains(TelegramDiagnosticConversationService.NewCodeButton, buttons);
    }

    [Fact]
    public async Task ValidManualPhoneIsSavedAsManualAndUnverified()
    {
        var harness = CreateHarness([]);

        await harness.Adapter.HandleAsync(Update("Ввести другой номер"));
        var response = await harness.Adapter.HandleAsync(Update("+998 90 123 45 67"));
        var user = await harness.UserStore.GetByChatIdAsync(7);

        Assert.Contains("номер сохранен", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(user?.HasPhoneNumber);
        Assert.False(user?.PhoneNumberVerified);
        Assert.Equal(TelegramUserPhoneNumberSource.Manual, user?.PhoneNumberSource);
    }

    [Fact]
    public async Task InvalidManualPhoneKeepsWaitingForPhoneNumber()
    {
        var harness = CreateHarness([]);

        await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.ManualPhoneButton));
        var response = await harness.Adapter.HandleAsync(Update("12345"));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Не получилось распознать номер", response.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramConversationState.WaitingForPhoneNumber, session?.State);
    }

    [Fact]
    public async Task NewCodeButtonCancelsManualPhoneInput()
    {
        var harness = CreateHarness([]);

        await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.ManualPhoneButton));
        var response = await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.NewCodeButton));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Введите код ошибки", response.Text, StringComparison.Ordinal);
        Assert.Null(session);
    }

    [Theory]
    [InlineData("/phone")]
    [InlineData("Изменить номер")]
    public async Task ManualPhoneTextAliasesEnterWaitingForPhoneNumber(string text)
    {
        var harness = CreateHarness([]);

        var response = await harness.Adapter.HandleAsync(Update(text));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Введите номер телефона", response.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramConversationState.WaitingForPhoneNumber, session?.State);
    }

    [Fact]
    public async Task CancelButtonCancelsManualPhoneInput()
    {
        var harness = CreateHarness([]);

        await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.ManualPhoneButton));
        var response = await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.CancelButton));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Введите код ошибки", response.Text, StringComparison.Ordinal);
        Assert.Null(session);
    }

    [Fact]
    public async Task CodeTextDuringManualPhoneInputStartsNewDiagnosticScenario()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);

        await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.ManualPhoneButton));
        var response = await harness.Adapter.HandleAsync(Update("H5"));

        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NewTextCodeOverridesUnfinishedBrandSelection()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Gree", "E6", EquipmentCategory.VrfOutdoorUnit)
        ]);

        var h5 = await harness.Adapter.HandleAsync(Update("H5"));
        var e6 = await harness.Adapter.HandleAsync(Update("E6"));

        Assert.Contains("нескольких брендов", h5.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("E6", e6.Text, StringComparison.Ordinal);
        Assert.Contains("Что можно сделать безопасно", e6.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EngineerFinalResponseUsesTechnicalFormatter()
    {
        var harness = CreateHarness([Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)]);
        await harness.UserStore.AllowAsync(7, TelegramUserRole.Engineer);

        var response = await harness.Adapter.HandleAsync(Update("H5"));

        Assert.Contains("Уверенность:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Безопасность:", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OwnerAdminCommandWorksWithActiveSession()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ], Options() with { BootstrapOwnerChatId = 7 });
        await harness.Adapter.HandleAsync(Update("H5"));

        var response = await harness.Adapter.HandleAsync(Update("/admin users"));

        Assert.Contains("Пользователи Telegram", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Выберите бренд", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ContactMessageSavesPhoneAndPreservesActiveSession()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);
        await harness.Adapter.HandleAsync(Update("H5"));

        var contact = await harness.Adapter.HandleAsync(Update(
            text: null,
            contactPhone: "+998901234567",
            contactUserId: 11));
        var afterContact = await harness.Adapter.HandleAsync(Update("Gree"));

        Assert.Contains("номер сохранен", contact.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Выберите бренд", contact.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Что можно сделать безопасно", afterContact.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManualPhoneFlowPreservesActiveSessionAndRestoresPrompt()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);
        await harness.Adapter.HandleAsync(Update("H5"));

        var phonePrompt = await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.ManualPhoneButton));
        var saved = await harness.Adapter.HandleAsync(Update("998901234567"));
        var afterPhone = await harness.Adapter.HandleAsync(Update("Gree"));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var session = await harness.SessionStore.GetByTelegramUserIdAsync(user!.Id);

        Assert.Contains("Введите номер телефона", phonePrompt.Text, StringComparison.Ordinal);
        Assert.Contains("Продолжим диагностику", saved.Text, StringComparison.Ordinal);
        Assert.Contains("Выберите бренд", saved.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Что можно сделать безопасно", afterPhone.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramConversationState.ShowingResult, session?.State);
    }

    [Fact]
    public async Task UnknownCodeReturnsRussianNotFoundGuidanceAndMainKeyboard()
    {
        var harness = CreateHarness([]);

        var response = await harness.Adapter.HandleAsync(Update("ZZ99"));

        Assert.Contains("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(TelegramDiagnosticConversationService.NewCodeButton, ButtonTexts(response));
    }

    [Fact]
    public async Task SessionPersistsBetweenAdapterInstances()
    {
        var userStore = new InMemoryTelegramUserStore();
        var sessionStore = new InMemoryTelegramConversationSessionStore();
        var diagnostics = new FakeDiagnosticsService([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);
        var facade = new StaticFacade();
        var first = CreateAdapter(userStore, sessionStore, diagnostics, facade, Options());
        var second = CreateAdapter(userStore, sessionStore, diagnostics, facade, Options());

        await first.HandleAsync(Update("H5"));
        var response = await second.HandleAsync(Update("Gree"));

        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
        Assert.Equal(1, facade.CallCount);
    }

    private static Harness CreateHarness(
        IReadOnlyList<EquipmentErrorCodeSummaryDto> summaries,
        EquipmentDiagnosticTelegramOptions? options = null)
    {
        var userStore = new InMemoryTelegramUserStore();
        var sessionStore = new InMemoryTelegramConversationSessionStore();
        var diagnostics = new FakeDiagnosticsService(summaries);
        var facade = new StaticFacade();
        var adapter = CreateAdapter(userStore, sessionStore, diagnostics, facade, options ?? Options());
        return new Harness(adapter, userStore, sessionStore, facade);
    }

    private static EquipmentDiagnosticTelegramAdapter CreateAdapter(
        ITelegramUserStore userStore,
        ITelegramConversationSessionStore sessionStore,
        IEquipmentDiagnosticsService diagnostics,
        IEquipmentDiagnosticBotFacade facade,
        EquipmentDiagnosticTelegramOptions options)
    {
        var parser = new EquipmentDiagnosticTelegramMessageParser();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var access = new TelegramUserAccessService(userStore, options);
        var conversation = new TelegramDiagnosticConversationService(
            sessionStore,
            diagnostics,
            facade,
            userStore,
            parser,
            formatter,
            options);

        return new EquipmentDiagnosticTelegramAdapter(
            facade,
            parser,
            formatter,
            options,
            access,
            userStore,
            conversation);
    }

    private static EquipmentDiagnosticTelegramOptions Options() => new()
    {
        IsEnabled = true,
        MaxMessageLength = 1000,
        DefaultManufacturer = "Gree"
    };

    private static EquipmentDiagnosticTelegramUpdate Update(
        string? text,
        string? contactPhone = null,
        long? contactUserId = null) =>
        new(
            UpdateId: 1,
            ChatId: 7,
            Username: "operator",
            Text: text,
            UserId: 11,
            ContactPhoneNumber: contactPhone,
            ContactUserId: contactUserId);

    private static EquipmentErrorCodeSummaryDto Summary(
        string manufacturer,
        string code,
        EquipmentCategory category) =>
        new(
            manufacturer,
            "GMV",
            null,
            code,
            $"{code} title",
            $"{code} meaning",
            "Service review",
            category,
            DiagnosticConfidence.Low,
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
        InMemoryTelegramConversationSessionStore SessionStore,
        StaticFacade Facade);

    private sealed class FakeDiagnosticsService(
        IReadOnlyList<EquipmentErrorCodeSummaryDto> summaries) : IEquipmentDiagnosticsService
    {
        public Task<IReadOnlyList<EquipmentErrorCodeSummaryDto>> SearchErrorCodesAsync(
            SearchEquipmentErrorCodesQuery query,
            CancellationToken cancellationToken)
        {
            var results = summaries
                .Where(summary => string.IsNullOrWhiteSpace(query.ErrorCode) ||
                    string.Equals(summary.Code, query.ErrorCode, StringComparison.OrdinalIgnoreCase))
                .Where(summary => string.IsNullOrWhiteSpace(query.Manufacturer) ||
                    string.Equals(summary.Manufacturer, query.Manufacturer, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyList<EquipmentErrorCodeSummaryDto>>(results);
        }

        public Task<EquipmentDiagnosticCaseDto?> GetDiagnosticCaseAsync(
            string manufacturer,
            string errorCode,
            string? series,
            string? modelCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<EquipmentDiagnosticCaseDto?>(null);

        public Task<EquipmentDiagnosticsCatalogIndexDto> GetCatalogIndexAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StaticFacade : IEquipmentDiagnosticBotFacade
    {
        public int CallCount { get; private set; }

        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new EquipmentDiagnosticBotResponse(
                EquipmentDiagnosticBotResponseStatus.Answer,
                $"{request.Manufacturer} {request.Code}",
                "Possible protected operating condition.",
                request.Manufacturer ?? "Gree",
                request.Code ?? "H5",
                null,
                new EquipmentDiagnosticBotObservedCodeContext(request.Code ?? "H5", request.Code ?? "H5", request.FreeText),
                new EquipmentDiagnosticBotAnswerCard(
                    $"{request.Manufacturer} {request.Code}",
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
}
