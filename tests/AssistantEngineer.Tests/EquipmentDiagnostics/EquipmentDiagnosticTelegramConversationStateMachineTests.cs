using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramConversationStateMachineTests
{
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 6, 17, 17, 30, 0, TimeSpan.Zero);
    private const string ConsumerSafeSummaryText =
        "Сработала защита оборудования. Точное значение зависит от модели и места отображения ошибки.";
    private const string TechnicalStoredSummary =
        "Gree GMV6 H5: защита инверторного вентилятора наружного блока по превышению тока.";

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
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var diagnosticCase = await harness.HistoryStore.GetLastForTelegramUserAsync(user!.Id);

        Assert.DoesNotContain("Выберите бренд", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
        Assert.Equal(1, harness.Facade.CallCount);
        Assert.NotNull(diagnosticCase);
        Assert.Equal(TelegramDiagnosticCaseStatus.Completed, diagnosticCase.Status);
        Assert.Equal("H5", diagnosticCase.Code);
        Assert.Equal("Gree", diagnosticCase.Manufacturer);
        Assert.Equal(TelegramDiagnosticCaseResponseMode.Consumer, diagnosticCase.ResponseMode);
        Assert.False(diagnosticCase.PhoneWasSaved);
        Assert.Null(diagnosticCase.PhoneNumberSource);
        Assert.Contains("protected operating condition", diagnosticCase.ResultSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Что можно сделать безопасно", diagnosticCase.ResultSummary, StringComparison.OrdinalIgnoreCase);
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
        Assert.Empty(await harness.HistoryStore.GetLatestForTelegramUserAsync(user.Id, 5));
    }

    [Fact]
    public async Task StartHelpMePhoneAndIntermediatePromptsDoNotCreateDiagnosticCase()
    {
        var harness = CreateHarness([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);

        await harness.Adapter.HandleAsync(Update("/start"));
        await harness.Adapter.HandleAsync(Update("/help"));
        await harness.Adapter.HandleAsync(Update("/me"));
        await harness.Adapter.HandleAsync(Update("/phone"));
        await harness.Adapter.HandleAsync(Update("H5"));
        var user = await harness.UserStore.GetByChatIdAsync(7);

        Assert.NotNull(user);
        Assert.Empty(await harness.HistoryStore.GetLatestForTelegramUserAsync(user.Id, 5));
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
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var diagnosticCase = await harness.HistoryStore.GetLastForTelegramUserAsync(user!.Id);

        Assert.Contains("Уверенность:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Безопасность:", response.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramDiagnosticCaseResponseMode.Technical, diagnosticCase?.ResponseMode);
    }

    [Fact]
    public async Task InstallerFinalResponseAndLastUseTechnicalMode()
    {
        var harness = CreateHarness([Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)]);
        await harness.UserStore.AllowAsync(7, TelegramUserRole.Installer);

        var response = await harness.Adapter.HandleAsync(Update("H5"));
        var last = await harness.Adapter.HandleAsync(Update("/last"));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var diagnosticCase = await harness.HistoryStore.GetLastForTelegramUserAsync(user!.Id);

        Assert.Contains("Уверенность:", response.Text, StringComparison.Ordinal);
        Assert.Equal(TelegramDiagnosticCaseResponseMode.Technical, diagnosticCase?.ResponseMode);
        Assert.Contains("H5", last.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(ConsumerSafeSummaryText, last.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallerCanUseNormalConversationCommands()
    {
        var harness = CreateHarness([]);
        await harness.UserStore.AllowAsync(7, TelegramUserRole.Installer);

        var start = await harness.Adapter.HandleAsync(Update("/start"));
        var fresh = await harness.Adapter.HandleAsync(Update("/new"));
        var phone = await harness.Adapter.HandleAsync(Update("/phone"));
        var history = await harness.Adapter.HandleAsync(Update("/history"));
        var last = await harness.Adapter.HandleAsync(Update("/last"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, start.ResponseKind);
        Assert.Contains("Введите код ошибки", fresh.Text, StringComparison.Ordinal);
        Assert.Contains("Введите номер телефона", phone.Text, StringComparison.Ordinal);
        Assert.Contains("История пока пустая", history.Text, StringComparison.Ordinal);
        Assert.Contains("Отправьте код ошибки", last.Text, StringComparison.Ordinal);
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
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var diagnosticCase = await harness.HistoryStore.GetLastForTelegramUserAsync(user!.Id);

        Assert.Contains("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(TelegramDiagnosticConversationService.NewCodeButton, ButtonTexts(response));
        Assert.Equal(TelegramDiagnosticCaseStatus.NotFound, diagnosticCase?.Status);
        Assert.Equal("ZZ99", diagnosticCase?.Code);
        Assert.Equal(0, diagnosticCase?.CandidateCount);
    }

    [Fact]
    public async Task CompletedDiagnosticStoresPhoneMetadataWithoutPhoneNumberOrFullResponse()
    {
        var harness = CreateHarness([Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)]);
        await harness.UserStore.GetOrCreateConsumerAsync(Update("/start"));
        await harness.UserStore.SavePhoneAsync(7, "+998901234567", verified: false, TelegramUserPhoneNumberSource.Manual, DateTimeOffset.UtcNow);

        await harness.Adapter.HandleAsync(Update("H5"));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var diagnosticCase = await harness.HistoryStore.GetLastForTelegramUserAsync(user!.Id);
        var stored = string.Join(" ", diagnosticCase!.Code, diagnosticCase.Manufacturer, diagnosticCase.ResultSummary, diagnosticCase.NormalizedRequestJson, diagnosticCase.PhoneNumberSource);

        Assert.True(diagnosticCase.PhoneWasSaved);
        Assert.Equal(TelegramUserPhoneNumberSource.Manual, diagnosticCase.PhoneNumberSource);
        Assert.DoesNotContain("+998901234567", stored, StringComparison.Ordinal);
        Assert.DoesNotContain("Что можно сделать безопасно", stored, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Передайте мастеру", stored, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompletedDiagnosticStoresTelegramContactPhoneSourceWithoutPhoneNumber()
    {
        var harness = CreateHarness([Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)]);
        await harness.UserStore.GetOrCreateConsumerAsync(Update("/start"));
        await harness.UserStore.SavePhoneAsync(7, "+998901234567", verified: true, TelegramUserPhoneNumberSource.TelegramContact, DateTimeOffset.UtcNow);

        await harness.Adapter.HandleAsync(Update("H5"));
        var user = await harness.UserStore.GetByChatIdAsync(7);
        var diagnosticCase = await harness.HistoryStore.GetLastForTelegramUserAsync(user!.Id);
        var stored = string.Join(" ", diagnosticCase!.Code, diagnosticCase.Manufacturer, diagnosticCase.ResultSummary, diagnosticCase.NormalizedRequestJson, diagnosticCase.PhoneNumberSource);

        Assert.True(diagnosticCase.PhoneWasSaved);
        Assert.Equal(TelegramUserPhoneNumberSource.TelegramContact, diagnosticCase.PhoneNumberSource);
        Assert.DoesNotContain("+998901234567", stored, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HistoryShowsLatestFiveOwnCasesAndHidesOtherUsers()
    {
        var harness = CreateHarness([Summary("Gree", "H1", EquipmentCategory.VrfOutdoorUnit)]);
        var ownUser = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start", chatId: 7));
        var otherUser = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start", chatId: 8));
        var history = CreateHistoryService(harness.HistoryStore);

        await history.RecordCompletedAsync(Access(ownUser), null, Response("Gree", "H1", "one"), "Gree", "H1", "Наружный блок", "Плата/LED", 1);
        await history.RecordCompletedAsync(Access(ownUser), null, Response("Gree", "H2", "two"), "Gree", "H2", "Наружный блок", "Плата/LED", 1);
        await history.RecordCompletedAsync(Access(ownUser), null, Response("Gree", "H3", "three"), "Gree", "H3", "Наружный блок", "Плата/LED", 1);
        await history.RecordCompletedAsync(Access(ownUser), null, Response("Gree", "H4", "four"), "Gree", "H4", "Наружный блок", "Плата/LED", 1);
        await history.RecordCompletedAsync(Access(ownUser), null, Response("Gree", "H5", "five"), "Gree", "H5", "Наружный блок", "Плата/LED", 1);
        await history.RecordCompletedAsync(Access(ownUser), null, Response("Gree", "H6", "six"), "Gree", "H6", "Наружный блок", "Плата/LED", 1);
        await history.RecordCompletedAsync(Access(otherUser), null, Response("Daikin", "E6", "other"), "Daikin", "E6", "Наружный блок", "Плата/LED", 1);

        var response = await harness.Adapter.HandleAsync(Update("/history", chatId: 7));

        Assert.Contains("История диагностик", response.Text, StringComparison.Ordinal);
        Assert.Contains("1. Gree H6", response.Text, StringComparison.Ordinal);
        Assert.Contains("5. Gree H2", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree H1", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Daikin E6", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HistoryAndLastEmptyStatesAreRussian()
    {
        var harness = CreateHarness([]);

        var history = await harness.Adapter.HandleAsync(Update("/history"));
        var last = await harness.Adapter.HandleAsync(Update("/last"));

        Assert.Contains("История пока пустая", history.Text, StringComparison.Ordinal);
        Assert.Contains("Отправьте код ошибки", last.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LastShowsLatestOwnCompletedOrNotFoundCase()
    {
        var harness = CreateHarness([]);
        var user = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start"));
        var history = CreateHistoryService(harness.HistoryStore);

        await history.RecordCompletedAsync(Access(user), null, Response("Gree", "H5", "Saved short summary."), "Gree", "H5", null, null, 1);
        var completed = await harness.Adapter.HandleAsync(Update("/last"));
        await history.RecordNotFoundAsync(Access(user), null, "ZZ99", "Gree", 0);
        var notFound = await harness.Adapter.HandleAsync(Update("/last"));

        Assert.Contains("Последняя диагностика", completed.Text, StringComparison.Ordinal);
        Assert.Contains("Gree H5", completed.Text, StringComparison.Ordinal);
        Assert.Contains(ConsumerSafeSummaryText, completed.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Saved short summary", completed.Text, StringComparison.Ordinal);
        Assert.Contains("Последний запрос", notFound.Text, StringComparison.Ordinal);
        Assert.Contains("Gree ZZ99", notFound.Text, StringComparison.Ordinal);
        Assert.Contains("точная расшифровка не найдена", notFound.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConsumerLastHidesStoredEnglishTechnicalSummaryAndFormatsLocalTime()
    {
        var harness = CreateHarness([]);
        var user = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start"));
        await CreateCaseAsync(
            harness.HistoryStore,
            user,
            TelegramDiagnosticCaseStatus.Completed,
            "H5",
            "Gree",
            TechnicalStoredSummary,
            new DateTimeOffset(2026, 6, 17, 17, 20, 39, TimeSpan.Zero));

        var response = await harness.Adapter.HandleAsync(Update("/last"));

        Assert.Contains("Дата: 17.06.2026 22:20", response.Text, StringComparison.Ordinal);
        Assert.Contains(ConsumerSafeSummaryText, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("GMV protection alarm", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Preliminary diagnostic", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Confidence", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalDecisionTrace", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EngineerLastShowsStoredTechnicalSummary()
    {
        var harness = CreateHarness([]);
        await harness.UserStore.AllowAsync(7, TelegramUserRole.Engineer);
        var user = await harness.UserStore.GetByChatIdAsync(7);
        await CreateCaseAsync(
            harness.HistoryStore,
            user!,
            TelegramDiagnosticCaseStatus.Completed,
            "H5",
            "Gree",
            TechnicalStoredSummary,
            new DateTimeOffset(2026, 6, 17, 17, 20, 39, TimeSpan.Zero),
            TelegramDiagnosticCaseResponseMode.Technical,
            TelegramUserRole.Engineer);

        var response = await harness.Adapter.HandleAsync(Update("/last"));

        Assert.Contains("Дата: 17.06.2026 22:20", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree H5", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("GMV protection alarm", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("InternalDecisionTrace", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EngineerLastNormalizesLocalizedPossibleMeaning()
    {
        var store = new InMemoryTelegramDiagnosticCaseStore();
        var userStore = new InMemoryTelegramUserStore();
        await userStore.AllowAsync(7, TelegramUserRole.Engineer);
        var user = await userStore.GetByChatIdAsync(7);
        await CreateCaseAsync(
            store,
            user!,
            TelegramDiagnosticCaseStatus.Completed,
            "C0",
            "Gree",
            "Stored summary.",
            FixedNowUtc,
            TelegramDiagnosticCaseResponseMode.Technical,
            TelegramUserRole.Engineer);
        var history = new TelegramDiagnosticHistoryService(
            store,
            new TelegramDisplayTimeFormatter(
                Options(),
                new FixedTimeProvider(FixedNowUtc)),
            new DuplicateSummaryLocalizationSource());

        var response = await history.FormatLastAsync(user!);

        Assert.Contains("сообщение о связи и адресации", response, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("связи связи", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HistoryFormatsTodayYesterdayAndOlderDatesInDisplayTimeZone()
    {
        var harness = CreateHarness([]);
        var user = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start"));
        await CreateCaseAsync(harness.HistoryStore, user, TelegramDiagnosticCaseStatus.Completed, "H1", "Gree", "one", new DateTimeOffset(2026, 6, 17, 17, 20, 0, TimeSpan.Zero));
        await CreateCaseAsync(harness.HistoryStore, user, TelegramDiagnosticCaseStatus.Completed, "H2", "Gree", "two", new DateTimeOffset(2026, 6, 16, 17, 17, 0, TimeSpan.Zero));
        await CreateCaseAsync(harness.HistoryStore, user, TelegramDiagnosticCaseStatus.Completed, "H3", "Gree", "three", new DateTimeOffset(2026, 6, 15, 13, 10, 0, TimeSpan.Zero));

        var response = await harness.Adapter.HandleAsync(Update("/history"));

        Assert.Contains("Gree H1 — сегодня 22:20", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree H2 — вчера 22:17", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree H3 — 15.06.2026 18:10", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidDisplayTimeZoneFallsBackToTashkentAndLogsWarning()
    {
        var logger = new CapturingLogger<TelegramDisplayTimeFormatter>();
        var harness = CreateHarness([], Options() with { DisplayTimeZone = "Invalid/Zone" }, logger);
        var user = await harness.UserStore.GetOrCreateConsumerAsync(Update("/start"));
        await CreateCaseAsync(
            harness.HistoryStore,
            user,
            TelegramDiagnosticCaseStatus.Completed,
            "H5",
            "Gree",
            "summary",
            new DateTimeOffset(2026, 6, 17, 17, 20, 0, TimeSpan.Zero));

        var response = await harness.Adapter.HandleAsync(Update("/history"));

        Assert.Contains("сегодня 22:20", response.Text, StringComparison.Ordinal);
        Assert.Contains(logger.Messages, message => message.Contains("Falling back", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Invalid/Zone", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HistoryButtonBehavesLikeHistoryCommand()
    {
        var harness = CreateHarness([Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit)]);
        await harness.Adapter.HandleAsync(Update("H5"));

        var response = await harness.Adapter.HandleAsync(Update(TelegramDiagnosticConversationService.HistoryButton));

        Assert.Contains("История диагностик", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree H5", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SessionPersistsBetweenAdapterInstances()
    {
        var userStore = new InMemoryTelegramUserStore();
        var sessionStore = new InMemoryTelegramConversationSessionStore();
        var historyStore = new InMemoryTelegramDiagnosticCaseStore();
        var diagnostics = new FakeDiagnosticsService([
            Summary("Gree", "H5", EquipmentCategory.VrfOutdoorUnit),
            Summary("Daikin", "H5", EquipmentCategory.VrfOutdoorUnit)
        ]);
        var facade = new StaticFacade();
        var first = CreateAdapter(userStore, sessionStore, historyStore, diagnostics, facade, Options());
        var second = CreateAdapter(userStore, sessionStore, historyStore, diagnostics, facade, Options());

        await first.HandleAsync(Update("H5"));
        var response = await second.HandleAsync(Update("Gree"));

        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
        Assert.Equal(1, facade.CallCount);
    }

    private static Harness CreateHarness(
        IReadOnlyList<EquipmentErrorCodeSummaryDto> summaries,
        EquipmentDiagnosticTelegramOptions? options = null,
        CapturingLogger<TelegramDisplayTimeFormatter>? logger = null)
    {
        var userStore = new InMemoryTelegramUserStore();
        var sessionStore = new InMemoryTelegramConversationSessionStore();
        var historyStore = new InMemoryTelegramDiagnosticCaseStore();
        var diagnostics = new FakeDiagnosticsService(summaries);
        var facade = new StaticFacade();
        var adapter = CreateAdapter(userStore, sessionStore, historyStore, diagnostics, facade, options ?? Options(), logger);
        return new Harness(adapter, userStore, sessionStore, historyStore, facade);
    }

    private static EquipmentDiagnosticTelegramAdapter CreateAdapter(
        ITelegramUserStore userStore,
        ITelegramConversationSessionStore sessionStore,
        ITelegramDiagnosticCaseStore historyStore,
        IEquipmentDiagnosticsService diagnostics,
        IEquipmentDiagnosticBotFacade facade,
        EquipmentDiagnosticTelegramOptions options,
        CapturingLogger<TelegramDisplayTimeFormatter>? logger = null)
    {
        var parser = new EquipmentDiagnosticTelegramMessageParser();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var access = new TelegramUserAccessService(userStore, options);
        var history = CreateHistoryService(historyStore, options, logger: logger);
        var conversation = new TelegramDiagnosticConversationService(
            sessionStore,
            diagnostics,
            facade,
            new AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json.JsonErrorKnowledgeLocalizationSource(),
            userStore,
            parser,
            formatter,
            options,
            history);

        return new EquipmentDiagnosticTelegramAdapter(
            facade,
            parser,
            formatter,
            options,
            access,
            userStore,
            conversation,
            history);
    }

    private static EquipmentDiagnosticTelegramOptions Options() => new()
    {
        IsEnabled = true,
        MaxMessageLength = 1000,
        DefaultManufacturer = "Gree",
        DisplayTimeZone = "Asia/Tashkent"
    };

    private static TelegramDiagnosticHistoryService CreateHistoryService(
        ITelegramDiagnosticCaseStore store,
        EquipmentDiagnosticTelegramOptions? options = null,
        DateTimeOffset? utcNow = null,
        CapturingLogger<TelegramDisplayTimeFormatter>? logger = null) =>
        new(
            store,
            new TelegramDisplayTimeFormatter(
                options ?? Options(),
                new FixedTimeProvider(utcNow ?? FixedNowUtc),
                logger));

    private static Task<TelegramDiagnosticCaseSnapshot> CreateCaseAsync(
        InMemoryTelegramDiagnosticCaseStore store,
        TelegramUserSnapshot user,
        TelegramDiagnosticCaseStatus status,
        string code,
        string? manufacturer,
        string? resultSummary,
        DateTimeOffset createdAt,
        TelegramDiagnosticCaseResponseMode responseMode = TelegramDiagnosticCaseResponseMode.Consumer,
        TelegramUserRole? userRole = null) =>
        store.CreateAsync(new TelegramDiagnosticCaseCreate(
            user.Id,
            null,
            status,
            userRole ?? user.Role,
            responseMode,
            code,
            manufacturer,
            null,
            null,
            resultSummary,
            null,
            1,
            false,
            null,
            createdAt));

    private static EquipmentDiagnosticTelegramUpdate Update(
        string? text,
        long chatId = 7,
        string? contactPhone = null,
        long? contactUserId = null) =>
        new(
            UpdateId: 1,
            ChatId: chatId,
            Username: "operator",
            Text: text,
            UserId: 11,
            ContactPhoneNumber: contactPhone,
            ContactUserId: contactUserId);

    private static TelegramUserAccessResult Access(TelegramUserSnapshot user) =>
        new(true, user, user.Role);

    private static EquipmentDiagnosticBotResponse Response(
        string manufacturer,
        string code,
        string summary) =>
        new(
            EquipmentDiagnosticBotResponseStatus.Answer,
            $"{manufacturer} {code}",
            "Short diagnostic message.",
            manufacturer,
            code,
            null,
            new EquipmentDiagnosticBotObservedCodeContext(code, code, null),
            new EquipmentDiagnosticBotAnswerCard(
                $"{manufacturer} {code}",
                summary,
                "Verification required.",
                [],
                [],
                [],
                [],
                []),
            null,
            null,
            new EquipmentDiagnosticBotSafetyCard("Qualified technician required.", []),
            VerificationRequired: false,
            Confidence: DiagnosticConfidence.Low,
            IsManualVerified: false,
            IsSeedKnowledge: true,
            OperatorNextSteps: [],
            Warnings: [],
            InternalDecisionTrace: null);

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
        InMemoryTelegramDiagnosticCaseStore HistoryStore,
        StaticFacade Facade);

    private sealed class DuplicateSummaryLocalizationSource : IErrorKnowledgeLocalizationSource
    {
        private readonly JsonErrorKnowledgeLocalizationSource _inner = new();

        public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => _inner.GetEntries();

        public ErrorKnowledgeLocalizationSelection? Select(
            EquipmentDiagnosticBotResponse response,
            string locale,
            ErrorKnowledgeAudience audience)
        {
            var selection = _inner.Select(response, locale, audience);
            return selection is null
                ? null
                : selection with
                {
                    Text = selection.Text with
                    {
                        Summary = "Код C0 классифицирован как сообщение о связи связи и адресации."
                    }
                };
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
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
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

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
