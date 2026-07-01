using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramManualLibraryTests
{
    [Fact]
    public async Task ConsumerSeesDiagnosticGuideButtonButGetsOnlyOwnerManualPolicy()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var diagnosis = await adapter.HandleAsync(Update("Gree GMV6 H5"));
        var action = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));
        var legacyAlias = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticManualButton));
        var callback = await adapter.HandleAsync(ManualCallback());

        Assert.True(HasButton(diagnosis, TelegramManualLibraryService.DiagnosticGuideButton));
        Assert.False(HasButton(diagnosis, TelegramManualLibraryService.ManualLibraryButton));
        Assert.Equal("HTML", action.ParseMode);
        Assert.Contains("<b>Руководство пока не добавлено</b>", action.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Руководство пока не добавлено</b>", legacyAlias.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Руководство пока не добавлено</b>", callback.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(action.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        Assert.DoesNotContain(callback.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        AssertNoDiagnosticSourceLeak(action);
        AssertNoDiagnosticSourceLeak(callback);
    }
    [Theory]
    [InlineData(TelegramUserRole.Installer)]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Owner)]
    public async Task TechnicalRolesSeeDiagnosticManualButtonForConcreteFoundGreeAnswer(TelegramUserRole role)
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, role);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV6 H5"));

        Assert.True(HasButton(response, TelegramManualLibraryService.DiagnosticGuideButton));
        Assert.False(HasButton(response, TelegramManualLibraryService.ManualLibraryButton));
        Assert.Contains("<b>Диагностика GREE H5</b>", response.Text, StringComparison.Ordinal);
        AssertNoDiagnosticSourceLeak(response);
    }

    [Fact]
    public async Task TechnicalDiagnosticKeyboardUsesOnlyContextualManualActionAndCompactRows()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV6 H5"));

        Assert.Collection(
            KeyboardRows(response),
            row => Assert.Equal(
                [TelegramDiagnosticConversationService.NewCodeButton, TelegramManualLibraryService.DiagnosticGuideButton],
                row),
            row => Assert.Equal(
                [TelegramDiagnosticConversationService.HistoryButton, TelegramDiagnosticConversationService.ServiceRequestButton],
                row),
            row => Assert.Equal([TelegramDiagnosticConversationService.RequestsButton], row));
        Assert.False(HasButton(response, TelegramManualLibraryService.ManualLibraryButton));
        Assert.Equal(
            1,
            Buttons(response).Count(button =>
                string.Equals(button, TelegramManualLibraryService.DiagnosticGuideButton, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task LibraryButtonIsVisibleOnlyForOwnerOrGrantedTechnicalUser()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var userStore = provider.GetRequiredService<ITelegramUserStore>();
        var accessStore = provider.GetRequiredService<ITelegramLibraryAccessStore>();

        var withoutGrant = await adapter.HandleAsync(Update("/start"));
        var user = await userStore.GetByChatIdAsync(7);
        Assert.NotNull(user);

        await accessStore.GrantAsync(user.Id, grantedByTelegramUserDatabaseId: user.Id);
        var withGrant = await adapter.HandleAsync(Update("/start"));

        Assert.False(HasButton(withoutGrant, TelegramManualLibraryService.LibraryButton));
        Assert.True(HasButton(withGrant, TelegramManualLibraryService.LibraryButton));
        Assert.False(HasButton(withGrant, TelegramManualLibraryService.ManualLibraryButton));
    }

    [Fact]
    public async Task OwnerCanOpenLibraryButAdminWithoutGrantGetsRequestOption()
    {
        using var ownerProvider = CreateProvider();
        await AllowAsync(ownerProvider, TelegramUserRole.Owner);
        var ownerAdapter = ownerProvider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var ownerLibrary = await ownerAdapter.HandleAsync(Update("/library"));

        using var adminProvider = CreateProvider();
        await AllowAsync(adminProvider, TelegramUserRole.Admin);
        var adminAdapter = adminProvider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var adminLibrary = await adminAdapter.HandleAsync(Update("/library"));

        Assert.Contains("Библиотека файлов", ownerLibrary.Text, StringComparison.Ordinal);
        Assert.Contains("Gree", InlineButtons(ownerLibrary), StringComparer.Ordinal);
        Assert.Contains("Доступ к библиотеке файлов не выдан", adminLibrary.Text, StringComparison.Ordinal);
        Assert.Contains("Запросить доступ", string.Join(" ", InlineButtons(adminLibrary)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FreshLibraryNavigationCallbacksDoNotReturnStaleAction()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var home = await adapter.HandleAsync(LibraryCallback("lib:open"));
        var gree = await adapter.HandleAsync(LibraryCallback("lib:brand:gree"));
        var remotes = await adapter.HandleAsync(LibraryCallback("lib:gree:section:controllers"));
        var access = await adapter.HandleAsync(LibraryCallback("lib:access"));
        var requests = await adapter.HandleAsync(LibraryCallback("lib:reqs"));
        var cancel = await adapter.HandleAsync(LibraryCallback("lib:cancel"));

        Assert.Contains("Gree", InlineButtons(home), StringComparer.Ordinal);
        Assert.Contains("Наружные", InlineButtons(gree), StringComparer.Ordinal);
        Assert.Contains("Пульты / Controllers", remotes.Text, StringComparison.Ordinal);
        Assert.Contains("Настенные", InlineButtons(remotes), StringComparer.Ordinal);
        Assert.Contains("Беспроводные ИК", InlineButtons(remotes), StringComparer.Ordinal);
        Assert.Contains("Управление доступом", access.Text, StringComparison.Ordinal);
        Assert.Contains("Запросов нет", requests.CallbackAnswerText, StringComparison.Ordinal);
        Assert.Contains("закрыта", cancel.Text, StringComparison.OrdinalIgnoreCase);
        AssertNotStale(home);
        AssertNotStale(gree);
        AssertNotStale(remotes);
        AssertNotStale(access);
        AssertNotStale(requests);
        AssertNotStale(cancel);
    }

    [Fact]
    public async Task GreeLibraryShowsUMatchAndErvSectionsWithVisibleDocumentBuckets()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();
        await bindingStore.UpsertAsync(new TelegramManualFileBinding(
            "gree-umatch-r32-service-manual",
            "telegram-file-id-umatch",
            "Gree U-Match R32 Service Manual EN 3.5-16kW.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "U-Match R32",
            Title: "Gree U-Match R32 Service Manual EN",
            DocumentType: TelegramLibraryDocumentType.ServiceManual,
            MinRole: TelegramUserRole.Engineer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false));
        await bindingStore.UpsertAsync(new TelegramManualFileBinding(
            "gree-erv-b-series-service-manual",
            "telegram-file-id-erv",
            "Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "ERV B Series",
            Title: "Gree ERV B Series Service Manual EN",
            DocumentType: TelegramLibraryDocumentType.ServiceManual,
            MinRole: TelegramUserRole.Engineer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false));
        await bindingStore.UpsertAsync(new TelegramManualFileBinding(
            "gree-umatch-hidden-installation-manual",
            "telegram-file-id-installation",
            "Gree U-Match Installation Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "U-Match R32",
            Title: "Gree U-Match Installation Manual EN",
            DocumentType: TelegramLibraryDocumentType.InstallationManual,
            MinRole: TelegramUserRole.Installer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false));

        var gree = await adapter.HandleAsync(LibraryCallback("lib:brand:gree"));
        var umatch = await adapter.HandleAsync(LibraryCallback("lib:gree:section:umatch"));
        var erv = await adapter.HandleAsync(LibraryCallback("lib:gree:section:erv"));
        var umatchService = await adapter.HandleAsync(LibraryCallback("lib:gree:section:umatch:service"));
        var ervService = await adapter.HandleAsync(LibraryCallback("lib:gree:section:erv:service"));

        Assert.Contains("Полупром / U-Match", InlineButtons(gree), StringComparer.Ordinal);
        Assert.Contains("Вентиляция ERV", InlineButtons(gree), StringComparer.Ordinal);
        Assert.Contains("📕 Сервисные мануалы", InlineButtons(umatch), StringComparer.Ordinal);
        Assert.Contains("📘 Руководства пользователя", InlineButtons(umatch), StringComparer.Ordinal);
        Assert.Contains("📕 Сервисные мануалы", InlineButtons(erv), StringComparer.Ordinal);
        Assert.Contains("📘 Руководства пользователя", InlineButtons(erv), StringComparer.Ordinal);
        Assert.DoesNotContain("Installation Manual", string.Join(" ", InlineButtons(umatch)), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gree U-Match R32 Service Manual EN", umatchService.Text, StringComparison.Ordinal);
        Assert.Contains("Gree ERV B Series Service Manual EN", ervService.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Installation", umatchService.Text, StringComparison.OrdinalIgnoreCase);
        AssertSafeLibraryFileButtons(umatchService);
        AssertSafeLibraryFileButtons(ervService);
    }

    [Fact]
    public async Task EmptyAccessRequestsScreenHasBackButtonAndBackEditsRoot()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var empty = await adapter.HandleAsync(LibraryCallback("lib:reqs", messageId: 7001));
        var back = await adapter.HandleAsync(LibraryCallback("lib:open", messageId: 7001));

        Assert.Contains("Запросов нет", empty.CallbackAnswerText, StringComparison.Ordinal);
        Assert.Contains("Назад", InlineButtons(empty), StringComparer.Ordinal);
        AssertSingleEdit(empty, 7001);
        AssertSingleEdit(back, 7001);
        Assert.Contains("Gree", InlineButtons(back), StringComparer.Ordinal);
    }

    [Fact]
    public async Task LibraryCallbackNavigationUsesEditMessageIdInsteadOfNewTextMessages()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var initial = await adapter.HandleAsync(Update("/library"));
        var navigation = new[]
        {
            await adapter.HandleAsync(LibraryCallback("lib:open", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:brand:gree", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:gree:outdoor", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:gree:outdoor:gmv6", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:gree:outdoor:gmv6:service", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:gree:section:controllers", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:reqs", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:access", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:cancel", messageId: 7001)),
            await adapter.HandleAsync(LibraryCallback("lib:brand:remotes", messageId: 7001))
        };

        Assert.Equal(1, TextSendCount(initial));
        Assert.Equal(0, EditTextCount(initial));
        Assert.All(navigation, response => AssertSingleEdit(response, 7001));
        Assert.Equal(0, navigation.Sum(TextSendCount));
        Assert.Equal(navigation.Length, navigation.Sum(EditTextCount));
        Assert.All(navigation, AssertNotStale);
    }

    [Fact]
    public async Task AccessRequestListShowsUserIdentityAndApproveNotifiesRequesterWithLibraryKeyboard()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound: outbound);
        await AllowAsync(provider, TelegramUserRole.Owner);
        await CreateUserAsync(provider, 42, 420, "Ravilya_Nur", "Равиля", null, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(LibraryCallback("lib:req", chatId: 42, userId: 420, username: "Ravilya_Nur", firstName: "Равиля"));
        var requests = await adapter.HandleAsync(LibraryCallback("lib:reqs"));
        var approved = await adapter.HandleAsync(LibraryCallback("lib:approve:1"));
        var requesterStart = await adapter.HandleAsync(Update("/start", chatId: 42, userId: 420, username: "Ravilya_Nur", firstName: "Равиля"));
        var notification = Assert.Single(outbound.Messages);

        Assert.Contains("Равиля", requests.Text, StringComparison.Ordinal);
        Assert.Contains("@Ravilya_Nur", requests.Text, StringComparison.Ordinal);
        Assert.Contains("Engineer", requests.Text, StringComparison.Ordinal);
        Assert.Contains("chat: 42", requests.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("chat 42, role", requests.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("одобрен", approved.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(42, notification.ChatId);
        Assert.True(
            notification.Text.Contains("выдан", StringComparison.OrdinalIgnoreCase) ||
            notification.Text.Contains("РІС‹РґР°РЅ", StringComparison.OrdinalIgnoreCase),
            notification.Text);
        Assert.Contains(TelegramManualLibraryService.LibraryButton, KeyboardButtons(notification.ReplyMarkup), StringComparer.Ordinal);
        Assert.True(HasButton(requesterStart, TelegramManualLibraryService.LibraryButton));
    }

    [Fact]
    public async Task RejectNotifiesRequesterWithoutLibraryKeyboard()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound: outbound);
        await AllowAsync(provider, TelegramUserRole.Owner);
        await CreateUserAsync(provider, 43, 430, "installer_user", "Монтаж", null, TelegramUserRole.Installer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(LibraryCallback("lib:req", chatId: 43, userId: 430, username: "installer_user", firstName: "Монтаж"));
        var rejected = await adapter.HandleAsync(LibraryCallback("lib:reject:1"));
        var notification = Assert.Single(outbound.Messages);

        Assert.Contains("отклон", rejected.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(43, notification.ChatId);
        Assert.True(
            notification.Text.Contains("отклон", StringComparison.OrdinalIgnoreCase) ||
            notification.Text.Contains("РѕС‚РєР»РѕРЅ", StringComparison.OrdinalIgnoreCase),
            notification.Text);
        Assert.DoesNotContain(TelegramManualLibraryService.LibraryButton, KeyboardButtons(notification.ReplyMarkup), StringComparer.Ordinal);
    }

    [Fact]
    public async Task AdminCannotApproveLibraryAccessRequest()
    {
        var outbound = new FakeOutbound();
        using var provider = CreateProvider(outbound: outbound);
        await AllowAsync(provider, TelegramUserRole.Admin);
        await CreateUserAsync(provider, 44, 440, "engineer_user", "Инженер", null, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(LibraryCallback("lib:req", chatId: 44, userId: 440, username: "engineer_user", firstName: "Инженер"));
        var denied = await adapter.HandleAsync(LibraryCallback("lib:approve:1"));

        Assert.Contains("владельцу", denied.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(outbound.Messages);
    }

    [Fact]
    public async Task ConsumerDiagnosticKeyboardShowsSafeGuideButNoLibraryAction()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV6 H5"));

        Assert.Collection(
            KeyboardRows(response),
            row => Assert.Equal(
                [TelegramDiagnosticConversationService.NewCodeButton, TelegramManualLibraryService.DiagnosticGuideButton],
                row),
            row => Assert.Equal(
                [TelegramDiagnosticConversationService.HistoryButton, TelegramDiagnosticConversationService.ServiceRequestButton],
                row),
            row => Assert.Equal([TelegramDiagnosticConversationService.RequestsButton], row));
        Assert.True(HasButton(response, TelegramManualLibraryService.DiagnosticGuideButton));
        Assert.False(HasButton(response, TelegramManualLibraryService.ManualLibraryButton));
    }
    [Fact]
    public async Task DiagnosticManualButtonIsAbsentForAmbiguityAndNotFound()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var ambiguity = await adapter.HandleAsync(Update("Gree n2"));
        var notFound = await adapter.HandleAsync(Update("Gree GMV9 Flex n2"));

        Assert.False(HasButton(ambiguity, TelegramManualLibraryService.DiagnosticGuideButton));
        Assert.False(HasButton(notFound, TelegramManualLibraryService.DiagnosticGuideButton));
        Assert.False(HasButton(ambiguity, TelegramManualLibraryService.ManualLibraryButton));
        Assert.False(HasButton(notFound, TelegramManualLibraryService.ManualLibraryButton));
    }

    [Fact]
    public async Task TechnicalDiagnosticGuideActionUsesLastConcreteDiagnosticAndFallsBackWhenOwnerManualMissing()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("Gree GMV9 Flex E0"));
        var response = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));

        Assert.Equal("HTML", response.ParseMode);
        Assert.Contains("<b>Руководство пока не добавлено</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV9 Flex / E0", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(response.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        Assert.True(HasButton(response, TelegramManualLibraryService.DiagnosticGuideButton));
        Assert.False(HasButton(response, TelegramManualLibraryService.ManualLibraryButton));
        AssertNoDiagnosticSourceLeak(response);
    }
    [Fact]
    public async Task DiagnosticGuideActionDoesNotSendServiceManualBinding()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await bindingStore.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-service-manual",
            "telegram-file-id-gmv6",
            "Gree GMV6 Service Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Admin.ToString(),
            Brand: "Gree",
            Series: "GMV6"));
        await adapter.HandleAsync(Update("Gree GMV6 d1"));
        var response = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));
        var documents = response.OutboundMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.DocumentFileId))
            .ToArray();

        Assert.Contains("<b>Руководство пока не добавлено</b>", response.Text, StringComparison.Ordinal);
        Assert.Empty(documents);
        Assert.DoesNotContain("forward", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertNoDiagnosticSourceLeak(response);
    }

    [Fact]
    public async Task DiagnosticGuideActionSendsOwnerManualBindingWithProtectContent()
    {
        using var provider = CreateProvider(TempBindingPath());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await bindingStore.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-owner-manual",
            "telegram-file-id-gmv6-owner",
            "Gree GMV6 Owner Manual.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "GMV6",
            Title: "Gree GMV6 Owner Manual",
            DocumentType: TelegramLibraryDocumentType.OwnerManual,
            MinRole: TelegramUserRole.Consumer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: true));

        await adapter.HandleAsync(Update("Gree GMV6 d1"));
        var response = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));
        var documents = response.OutboundMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.DocumentFileId))
            .ToArray();

        Assert.Contains("<b>Руководство по диагностике</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains(documents, message => message.DocumentFileId == "telegram-file-id-gmv6-owner" && message.ProtectContent);
        AssertNoDiagnosticSourceLeak(response);
    }
    [Fact]
    public async Task DiagnosticManualActionWithoutLastConcreteDiagnosticExplainsRequiredContext()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));

        Assert.Equal("HTML", response.ParseMode);
        Assert.Contains("<b>Мануал недоступен</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("Сначала выполните диагностику конкретного кода", response.Text, StringComparison.Ordinal);
        AssertNoDiagnosticSourceLeak(response);
    }

    [Fact]
    public async Task ConsumerCannotRequestManuals()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("/manuals"));

        Assert.Contains("только техническим ролям", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(response.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
    }

    [Theory]
    [InlineData(TelegramUserRole.Installer)]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Owner)]
    public async Task TechnicalRolesCanRequestKnownManualsAfterDiagnostic(TelegramUserRole role)
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, role);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("Gree GMV6 d1"));
        var response = await adapter.HandleAsync(Update("/manuals"));

        Assert.Contains("файлы пока не подключены", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Сервисное руководство GMV6 v2020.09 (GC202001-I)", response.Text, StringComparison.Ordinal);
        Assert.Contains("Сервисное руководство внутренних блоков GMV (GC202004-X)", response.Text, StringComparison.Ordinal);
        AssertNoSensitiveManualLeak(response.Text);
    }

    [Fact]
    public async Task MissingLastDiagnosticAsksToCheckCodeFirst()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("/manuals"));

        Assert.Contains("Сначала выполните диагностику", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExistingFileBindingsSendTelegramDocuments()
    {
        var bindingPath = TempBindingPath();
        using var provider = CreateProvider(bindingPath);
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var firstRegistration = await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv6-service-manual-2020-09",
            "telegram-file-id-gmv6",
            "Service Manual for GMV6 v_2020.09.pdf"));
        var secondRegistration = await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv-idu-service-manual",
            "telegram-file-id-idu",
            "SERVICE_MANUAL_GMV_IDU.pdf"));
        Assert.Contains("подключен", firstRegistration.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("подключен", secondRegistration.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(bindingPath), $"{firstRegistration.Text} | {secondRegistration.Text}");
        await adapter.HandleAsync(Update("Gree GMV6 d1"));

        var response = await adapter.HandleAsync(Update("/manuals"));
        var documents = response.OutboundMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.DocumentFileId))
            .ToArray();

        Assert.True(documents.Length == 2, response.Text);
        Assert.Contains(documents, document => document.DocumentFileId == "telegram-file-id-gmv6");
        Assert.Contains(documents, document => document.DocumentFileId == "telegram-file-id-idu");
        Assert.DoesNotContain("telegram-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.All(response.OutboundMessages, message => AssertNoSensitiveManualLeak(message.Text));
    }

    [Fact]
    public async Task PartialFileBindingsSendConnectedDocumentsAndListMissingManuals()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv6-service-manual-2020-09",
            "telegram-file-id-gmv6",
            "Service Manual for GMV6 v_2020.09.pdf"));
        await adapter.HandleAsync(Update("Gree GMV6 d1"));

        var response = await adapter.HandleAsync(Update("/manuals"));
        var documents = response.OutboundMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.DocumentFileId))
            .ToArray();

        Assert.Single(documents);
        Assert.Equal("telegram-file-id-gmv6", documents[0].DocumentFileId);
        Assert.Contains("Файл пока не подключен", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Сервисное руководство внутренних блоков GMV (GC202004-X)", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.All(response.OutboundMessages, message => AssertNoSensitiveManualLeak(message.Text));
    }

    [Theory]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Engineer)]
    public async Task NonOwnerCannotRegisterManualFile(TelegramUserRole role)
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, role);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv6-service-manual-2020-09",
            "telegram-file-id-gmv6",
            "Service Manual for GMV6 v_2020.09.pdf"));

        Assert.Contains("только админу или владельцу", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telegram-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Owner)]
    public async Task OwnerCanRegisterManualFile(TelegramUserRole role)
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, role);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv6-service-manual-2020-09",
            "telegram-file-id-gmv6",
            "Service Manual for GMV6 v_2020.09.pdf"));

        Assert.Contains("Файл руководства подключен", response.Text, StringComparison.Ordinal);
        Assert.Contains("Сервисное руководство GMV6 v2020.09 (GC202001-I)", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Consumer)]
    [InlineData(TelegramUserRole.Installer)]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Admin)]
    public async Task NonOwnerRolesCannotManageManualBindings(TelegramUserRole role)
    {
        using var provider = CreateProvider(TempBindingPath());
        if (role != TelegramUserRole.Consumer)
        {
            await AllowAsync(provider, role);
        }

        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var unregister = await adapter.HandleAsync(Update("/manual_unregister gree-gmv6-service-manual-2020-09"));
        var list = await adapter.HandleAsync(Update("/manual_bindings"));

        Assert.Contains("только админу или владельцу", unregister.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("только админу или владельцу", list.Text, StringComparison.OrdinalIgnoreCase);
        AssertNoSensitiveManualLeak(unregister.Text);
        AssertNoSensitiveManualLeak(list.Text);
    }

    [Fact]
    public async Task OwnerCanUnregisterManualFile()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv6-service-manual-2020-09",
            "telegram-file-id-gmv6",
            "Service Manual for GMV6 v_2020.09.pdf"));

        var removed = await adapter.HandleAsync(Update("/manual_unregister gree-gmv6-service-manual-2020-09"));
        var bindings = await adapter.HandleAsync(Update("/manual_bindings"));

        Assert.Contains("отключен", removed.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Сервисное руководство GMV6 v2020.09 (GC202001-I): файл не подключен", bindings.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-file-id", bindings.Text, StringComparison.OrdinalIgnoreCase);
        AssertNoSensitiveManualLeak(bindings.Text);
    }

    [Fact]
    public async Task ManualBindingsListIsSafeAndShowsOnlyDisplayNamesConnectionStateAndFileName()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv6-service-manual-2020-09",
            "telegram-file-id-gmv6",
            "/secret/Service Manual for GMV6 v_2020.09.pdf"));

        var response = await adapter.HandleAsync(Update("/manual_bindings"));

        Assert.Contains("Сервисное руководство GMV6 v2020.09 (GC202001-I): подключено; файл: Service Manual for GMV6 v_2020.09.pdf", response.Text, StringComparison.Ordinal);
        Assert.Contains("Сервисное руководство внутренних блоков GMV (GC202004-X): файл не подключен", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chatId", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertNoSensitiveManualLeak(response.Text);
    }

    [Theory]
    [InlineData(TelegramUserRole.Consumer)]
    [InlineData(TelegramUserRole.Installer)]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Admin)]
    public async Task NonOwnerRolesCannotStartManualBind(TelegramUserRole role)
    {
        using var provider = CreateProvider(TempBindingPath());
        if (role != TelegramUserRole.Consumer)
        {
            await AllowAsync(provider, role);
        }

        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("/manual_bind"));

        Assert.Contains("администратору", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(response.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
    }

    [Fact]
    public async Task ManualBindWaitsForPdfRejectsBadNameAndKeepsFlowActive()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var start = await adapter.HandleAsync(Update("/manual_bind"));
        var brand = await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        var section = await adapter.HandleAsync(ManualBindCallback("mb:sec:outdoor"));
        var series = await adapter.HandleAsync(ManualBindCallback("mb:s:gmv9-flex"));
        var selected = await adapter.HandleAsync(ManualBindCallback("mb:dt:service"));
        var nonDocument = await adapter.HandleAsync(Update("hello"));
        var badName = await adapter.HandleAsync(DocumentUpdate("telegram-file-id-bad", "Gree Manual.txt"));

        Assert.Contains("Gree", InlineButtons(start), StringComparer.Ordinal);
        Assert.Contains("Наружные", InlineButtons(brand), StringComparer.Ordinal);
        Assert.Contains("GMV9 Flex", InlineButtons(section), StringComparer.Ordinal);
        Assert.Contains("📕 Сервисные мануалы", string.Join(" ", InlineButtons(series)), StringComparison.Ordinal);
        Assert.Contains("📘 Руководства пользователя", string.Join(" ", InlineButtons(series)), StringComparison.Ordinal);
        Assert.DoesNotContain("📕 Service Manual", string.Join(" ", InlineButtons(series)), StringComparison.Ordinal);
        Assert.DoesNotContain("📘 Owner Manual", string.Join(" ", InlineButtons(series)), StringComparison.Ordinal);
        Assert.DoesNotContain("Installation Manual", string.Join(" ", InlineButtons(series)), StringComparison.Ordinal);
        Assert.Contains("Gree GMV9 Flex Service Manual EN Rev B.pdf", selected.Text, StringComparison.Ordinal);
        Assert.Contains("Ожидаю PDF-файл", nonDocument.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV9 Flex Service Manual EN Rev B.pdf", badName.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-file-id-bad", badName.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OutdoorDocumentTypeMenusLocalizeOwnerManualAndHideInstallationManual()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var productCallbacks = new[]
        {
            "lib:gree:outdoor:gmv6",
            "lib:gree:outdoor:gmv6-hr",
            "lib:gree:outdoor:gmv-mini",
            "lib:gree:outdoor:gmv-x",
            "lib:gree:outdoor:gmv9-flex"
        };

        foreach (var callback in productCallbacks)
        {
            var menu = await adapter.HandleAsync(LibraryCallback(callback));

            Assert.Equal(
                ["📕 Сервисные мануалы", "📘 Руководства пользователя", "Назад"],
                InlineButtons(menu));
            Assert.DoesNotContain("📘 Owner Manual", string.Join(" ", InlineButtons(menu)), StringComparison.Ordinal);
            Assert.DoesNotContain("🛠 Installation Manual", string.Join(" ", InlineButtons(menu)), StringComparison.Ordinal);
            Assert.DoesNotContain("Installation Manual", menu.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ManualBindDocumentTypeSelectorsHideInstallationManual()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        var outdoorSection = await adapter.HandleAsync(ManualBindCallback("mb:sec:outdoor"));
        var outdoorTypes = await adapter.HandleAsync(ManualBindCallback("mb:s:gmv6"));

        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        var indoorTypes = await adapter.HandleAsync(ManualBindCallback("mb:sec:indoor"));
        var combinedTypeButtons = InlineButtons(outdoorTypes).Concat(InlineButtons(indoorTypes)).ToArray();

        Assert.Contains("GMV6", InlineButtons(outdoorSection), StringComparer.Ordinal);
        Assert.Equal(
            ["📕 Сервисные мануалы", "📘 Руководства пользователя", "Отмена"],
            InlineButtons(outdoorTypes));
        Assert.Equal(
            ["📕 Сервисные мануалы", "📘 Руководства пользователя", "Отмена"],
            InlineButtons(indoorTypes));
        Assert.DoesNotContain("📘 Owner Manual", string.Join(" ", combinedTypeButtons), StringComparison.Ordinal);
        Assert.DoesNotContain("Installation Manual", string.Join(" ", combinedTypeButtons), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StaleInstallationManualCallbacksAreHandledWithoutSendingDocuments()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();
        await bindingStore.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv-mini-installation-manual",
            "telegram-file-id-installation",
            "Gree GMV Mini Slim Installation Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "GMV Mini",
            DocumentType: TelegramLibraryDocumentType.InstallationManual,
            MinRole: TelegramUserRole.Installer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false));
        var installation = Assert.Single(
            await bindingStore.ListAsync(),
            binding => binding.DocumentType == TelegramLibraryDocumentType.InstallationManual);

        var staleBucket = await adapter.HandleAsync(LibraryCallback("lib:gree:outdoor:gmv-mini:installation"));
        var directFile = await adapter.HandleAsync(LibraryCallback($"lib:f:{installation.Id}"));

        Assert.Equal(
            ["📕 Сервисные мануалы", "📘 Руководства пользователя", "Назад"],
            InlineButtons(staleBucket));
        Assert.DoesNotContain("Installation Manual", string.Join(" ", InlineButtons(staleBucket)), StringComparison.Ordinal);
        Assert.DoesNotContain(staleBucket.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        Assert.DoesNotContain(directFile.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        Assert.Contains("недоступен", directFile.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Owner)]
    public async Task OwnerCanBindSeriesManualAsLibraryOnlyServiceManual(TelegramUserRole role)
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, role);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        await adapter.HandleAsync(ManualBindCallback("mb:sec:outdoor"));
        await adapter.HandleAsync(ManualBindCallback("mb:s:gmv9-flex"));
        await adapter.HandleAsync(ManualBindCallback("mb:dt:service"));
        var candidate = await adapter.HandleAsync(DocumentUpdate(
            "telegram-file-id-gmv9-flex",
            "Gree GMV9 Flex Service Manual EN Rev B.pdf",
            "telegram-unique-gmv9-flex",
            123_456));
        var confirmed = await adapter.HandleAsync(ManualBindCallback("mb:c:bind"));
        await adapter.HandleAsync(Update("Gree GMV9 Flex E0"));
        var manual = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));
        var documents = manual.OutboundMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.DocumentFileId))
            .ToArray();
        var stored = await bindingStore.GetBySeriesAsync("Gree", "GMV9 Flex");

        Assert.Contains("GMV9 Flex", candidate.Text, StringComparison.Ordinal);
        Assert.Contains("Service Manual", candidate.Text, StringComparison.Ordinal);
        Assert.Contains("Файл", confirmed.Text, StringComparison.Ordinal);
        Assert.NotNull(stored);
        Assert.Equal("telegram-file-id-gmv9-flex", stored.TelegramFileId);
        Assert.Equal("telegram-unique-gmv9-flex", stored.TelegramFileUniqueId);
        Assert.Equal(123_456, stored.FileSize);
        Assert.Equal("Gree GMV9 Flex", $"{stored.Brand} {stored.Series}");
        Assert.Equal(TelegramLibraryDocumentType.ServiceManual, stored.DocumentType);
        Assert.Equal(TelegramUserRole.Engineer, stored.MinRole);
        Assert.False(stored.CanUseForDiagnostics);
        Assert.Empty(documents);
        Assert.Contains("<b>Руководство пока не добавлено</b>", manual.Text, StringComparison.Ordinal);

        var libraryFile = await adapter.HandleAsync(LibraryCallback("lib:file:gmv9flex"));
        var fileStatus = Assert.Single(libraryFile.OutboundMessages, message => string.IsNullOrWhiteSpace(message.DocumentFileId));
        var fileDocument = Assert.Single(libraryFile.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));

        Assert.Equal(99, fileStatus.EditMessageId);
        Assert.Equal("telegram-file-id-gmv9-flex", fileDocument.DocumentFileId);
        Assert.True(fileDocument.ProtectContent);
        Assert.Null(fileDocument.EditMessageId);
        Assert.Contains(
            libraryFile.OutboundMessages,
            message => message.DocumentFileId == "telegram-file-id-gmv9-flex" && message.ProtectContent);
        Assert.DoesNotContain("forward", manual.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("copy", manual.Text, StringComparison.OrdinalIgnoreCase);
        AssertNoDiagnosticSourceLeak(manual);
    }

    [Fact]
    public async Task OwnerCanBindControllerGuideIntoFreeSectionList()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        await adapter.HandleAsync(ManualBindCallback("mb:sec:controllers"));
        await adapter.HandleAsync(ManualBindCallback("mb:dt:controller"));
        await adapter.HandleAsync(DocumentUpdate(
            "telegram-file-id-controller",
            "Gree wired controller guide.pdf"));
        await adapter.HandleAsync(ManualBindCallback("mb:c:bind"));

        var section = await adapter.HandleAsync(LibraryCallback("lib:gree:section:controllers"));
        var wall = await adapter.HandleAsync(LibraryCallback("lib:c:wall"));
        var fileButton = Assert.Single(InlineButtonsWithCallbacks(wall), button => button.CallbackData.StartsWith("lib:f:", StringComparison.Ordinal));
        var file = await adapter.HandleAsync(LibraryCallback(fileButton.CallbackData));
        var fileDocument = Assert.Single(file.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));

        Assert.Contains("Настенные", InlineButtons(section), StringComparer.Ordinal);
        Assert.Contains("Gree wired controller guide.pdf", wall.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(InlineButtons(wall), button => button.Contains("Gree wired controller guide.pdf", StringComparison.Ordinal));
        Assert.True(Encoding.UTF8.GetByteCount(fileButton.CallbackData) <= 64, fileButton.CallbackData);
        Assert.DoesNotContain("GMV6", InlineButtons(section), StringComparer.Ordinal);
        Assert.Equal("telegram-file-id-controller", fileDocument.DocumentFileId);
        Assert.True(fileDocument.ProtectContent);
    }

    [Fact]
    public async Task GreeIndoorRootUsesTypedCategoriesAndRussianServiceManualLabel()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var indoor = await adapter.HandleAsync(LibraryCallback("lib:gree:section:indoor"));

        Assert.Contains("Внутренние блоки Gree", indoor.Text, StringComparison.Ordinal);
        Assert.Equal(
            ["Настенные", "Кассетные", "Канальные", "📕 Сервисные мануалы", "Назад"],
            InlineButtons(indoor));
        Assert.DoesNotContain("Прочее", InlineButtons(indoor), StringComparer.Ordinal);
        Assert.DoesNotContain("📕 Service Manual", string.Join(" ", InlineButtons(indoor)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GreeIndoorTypedCategoriesListOnlyClassifiedFilesWithShortSafeCallbacks()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();
        await SeedIndoorBindingAsync(bindingStore, "wall", "telegram-file-id-wall", "Gree_GMV_Wall_Mounted_Indoor_Unit_Owner_Manual_EN_B6B_B4B_C4B_C2B.pdf");
        await SeedIndoorBindingAsync(bindingStore, "cassette-td-a", "telegram-file-id-td-a", "Gree_GMV_One_way_Cassette_Indoor_Unit_Owner_Manual_EN_TD_A_22_56.pdf");
        await SeedIndoorBindingAsync(bindingStore, "cassette-t-c", "telegram-file-id-t-c", "Gree GMV Cassette Indoor Unit Owner Manual EN T-C 22-160.pdf");
        await SeedIndoorBindingAsync(bindingStore, "duct", "telegram-file-id-duct", "Gree_GMV_High_Static_Ducted_Indoor_Unit_Owner_Manual_EN_PHS_D_22.pdf");
        await SeedIndoorBindingAsync(
            bindingStore,
            "service",
            "telegram-file-id-service",
            "Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf",
            documentType: TelegramLibraryDocumentType.OwnerManual);
        await SeedIndoorBindingAsync(bindingStore, "unknown", "telegram-file-id-unknown", "Gree_GMV_Indoor_Unit_Owner_Manual_EN_Experimental.pdf");

        var wall = await adapter.HandleAsync(LibraryCallback("lib:i:wall"));
        var cassette = await adapter.HandleAsync(LibraryCallback("lib:i:cas"));
        var duct = await adapter.HandleAsync(LibraryCallback("lib:i:duc"));
        var service = await adapter.HandleAsync(LibraryCallback("lib:i:svc"));

        Assert.Contains("Gree_GMV_Wall_Mounted_Indoor_Unit_Owner_Manual_EN_B6B_B4B_C4B_C2B.pdf", wall.Text, StringComparison.Ordinal);
        Assert.Contains("TD_A_22_56.pdf", cassette.Text, StringComparison.Ordinal);
        Assert.Contains("T-C 22-160.pdf", cassette.Text, StringComparison.Ordinal);
        Assert.Contains("PHS_D_22.pdf", duct.Text, StringComparison.Ordinal);
        Assert.Contains("Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf", service.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Experimental", wall.Text + cassette.Text + duct.Text + service.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Service_Manual", duct.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Service_Manual", cassette.Text, StringComparison.OrdinalIgnoreCase);
        AssertSafeLibraryFileButtons(wall);
        AssertSafeLibraryFileButtons(cassette);
        AssertSafeLibraryFileButtons(duct);
        AssertSafeLibraryFileButtons(service);
    }

    [Fact]
    public async Task GreeControllerTypedCategoriesListOnlyClassifiedFilesWithShortSafeCallbacks()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();
        await SeedControllerBindingAsync(bindingStore, "xk46", "telegram-file-id-xk46", "Gree Wired Controller XK46 Owner Manual EN.pdf");
        await SeedControllerBindingAsync(bindingStore, "xe7a-23", "telegram-file-id-xe7a-23", "Gree Wired Controller XE7A-23H XE7A-23HC Owner Manual EN.pdf");
        await SeedControllerBindingAsync(bindingStore, "xe7a-24", "telegram-file-id-xe7a-24", "Gree Wired Controller XE7A-24H XE7A-24HC Owner Manual EN.pdf");
        await SeedControllerBindingAsync(bindingStore, "yap", "telegram-file-id-yap", "Gree Remote Controller YAP1F YV1L1 Owner Manual EN.pdf");
        await SeedControllerBindingAsync(bindingStore, "unknown", "telegram-file-id-unknown-controller", "Gree Controller Unknown Owner Manual EN.pdf");

        var root = await adapter.HandleAsync(LibraryCallback("lib:gree:section:controllers"));
        var wall = await adapter.HandleAsync(LibraryCallback("lib:c:wall"));
        var ir = await adapter.HandleAsync(LibraryCallback("lib:c:ir"));

        Assert.Equal(["Настенные", "Беспроводные ИК", "Назад"], InlineButtons(root));
        Assert.DoesNotContain("Прочее", InlineButtons(root), StringComparer.Ordinal);
        Assert.Contains("Gree Wired Controller XK46 Owner Manual EN.pdf", wall.Text, StringComparison.Ordinal);
        Assert.Contains("Gree Wired Controller XE7A-23H XE7A-23HC Owner Manual EN.pdf", wall.Text, StringComparison.Ordinal);
        Assert.Contains("Gree Wired Controller XE7A-24H XE7A-24HC Owner Manual EN.pdf", wall.Text, StringComparison.Ordinal);
        Assert.Contains("Gree Remote Controller YAP1F YV1L1 Owner Manual EN.pdf", ir.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown", wall.Text + ir.Text, StringComparison.OrdinalIgnoreCase);
        AssertSafeLibraryFileButtons(wall);
        AssertSafeLibraryFileButtons(ir);
    }

    [Fact]
    public async Task TypedLibraryFileDeliveryRechecksActiveVisibleAndRoleBeforeSend()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();
        await SeedIndoorBindingAsync(bindingStore, "owner", "telegram-file-id-owner", "Gree_GMV_Wall_Mounted_Indoor_Unit_Owner_Manual_EN_B6B_B4B_C4B_C2B.pdf");
        await SeedIndoorBindingAsync(
            bindingStore,
            "service",
            "telegram-file-id-service",
            "Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf",
            TelegramLibraryDocumentType.ServiceManual,
            TelegramUserRole.Engineer);
        await SeedControllerBindingAsync(bindingStore, "xk46", "telegram-file-id-controller", "Gree Wired Controller XK46 Owner Manual EN.pdf");
        await SeedIndoorBindingAsync(
            bindingStore,
            "inactive",
            "telegram-file-id-inactive",
            "Gree_GMV_Wall_Mounted_Indoor_Unit_Owner_Manual_EN_Inactive.pdf",
            isActive: false);
        await SeedIndoorBindingAsync(
            bindingStore,
            "hidden",
            "telegram-file-id-hidden",
            "Gree_GMV_Wall_Mounted_Indoor_Unit_Owner_Manual_EN_Hidden.pdf",
            isLibraryVisible: false);

        var ownerList = await adapter.HandleAsync(LibraryCallback("lib:i:wall"));
        var serviceList = await adapter.HandleAsync(LibraryCallback("lib:i:svc"));
        var controllerList = await adapter.HandleAsync(LibraryCallback("lib:c:wall"));
        var ownerFileCallback = Assert.Single(InlineButtonsWithCallbacks(ownerList), button => button.CallbackData.StartsWith("lib:f:", StringComparison.Ordinal)).CallbackData;
        var serviceFileCallback = Assert.Single(InlineButtonsWithCallbacks(serviceList), button => button.CallbackData.StartsWith("lib:f:", StringComparison.Ordinal)).CallbackData;
        var controllerFileCallback = Assert.Single(InlineButtonsWithCallbacks(controllerList), button => button.CallbackData.StartsWith("lib:f:", StringComparison.Ordinal)).CallbackData;

        var ownerSent = await adapter.HandleAsync(LibraryCallback(ownerFileCallback));
        await SetRoleAndGrantLibraryAsync(provider, TelegramUserRole.Installer);
        var serviceDeniedForInstaller = await adapter.HandleAsync(LibraryCallback(serviceFileCallback));
        var controllerSentForInstaller = await adapter.HandleAsync(LibraryCallback(controllerFileCallback));
        await SetRoleAndGrantLibraryAsync(provider, TelegramUserRole.Engineer);
        var serviceSentForEngineer = await adapter.HandleAsync(LibraryCallback(serviceFileCallback));
        await SetRoleAndGrantLibraryAsync(provider, TelegramUserRole.Consumer);
        var ownerDeniedForConsumer = await adapter.HandleAsync(LibraryCallback(ownerFileCallback));

        Assert.Contains("telegram-file-id-owner", ownerSent.OutboundMessages.Select(message => message.DocumentFileId), StringComparer.Ordinal);
        Assert.DoesNotContain("Inactive.pdf", ownerList.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Hidden.pdf", ownerList.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(serviceDeniedForInstaller.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        Assert.Contains("telegram-file-id-controller", controllerSentForInstaller.OutboundMessages.Select(message => message.DocumentFileId), StringComparer.Ordinal);
        Assert.Contains("telegram-file-id-service", serviceSentForEngineer.OutboundMessages.Select(message => message.DocumentFileId), StringComparer.Ordinal);
        Assert.DoesNotContain(ownerDeniedForConsumer.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
    }

    [Fact]
    public async Task ManualBindReplaceRequiresConfirmationAndCancelKeepsExistingBinding()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();
        await bindingStore.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-service-manual",
            "telegram-file-id-old",
            "Gree GMV6 Service Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "GMV6"));

        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        await adapter.HandleAsync(ManualBindCallback("mb:sec:outdoor"));
        await adapter.HandleAsync(ManualBindCallback("mb:s:gmv6"));
        await adapter.HandleAsync(ManualBindCallback("mb:dt:service"));
        var replacePrompt = await adapter.HandleAsync(DocumentUpdate("telegram-file-id-new", "Gree GMV6 Service Manual EN Rev C.pdf"));
        await adapter.HandleAsync(ManualBindCallback("mb:c:cancel"));
        var afterCancel = await bindingStore.GetBySeriesAsync("Gree", "GMV6");

        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        await adapter.HandleAsync(ManualBindCallback("mb:sec:outdoor"));
        await adapter.HandleAsync(ManualBindCallback("mb:s:gmv6"));
        await adapter.HandleAsync(ManualBindCallback("mb:dt:service"));
        await adapter.HandleAsync(DocumentUpdate("telegram-file-id-new", "Gree GMV6 Service Manual EN Rev C.pdf"));
        await adapter.HandleAsync(ManualBindCallback("mb:c:replace"));
        var afterReplace = await bindingStore.GetBySeriesAsync("Gree", "GMV6");

        Assert.Contains("Заменить", replacePrompt.Text, StringComparison.Ordinal);
        Assert.Equal("telegram-file-id-old", afterCancel?.TelegramFileId);
        Assert.Equal("telegram-file-id-new", afterReplace?.TelegramFileId);
    }

    [Fact]
    public async Task OwnerCanBindMultipleGmvMiniOwnerManualsWithoutDeactivatingServiceManual()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await bindingStore.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv-mini-service-manual",
            "telegram-file-id-service",
            "Gree GMV Mini Slim Service Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "GMV Mini",
            DocumentType: TelegramLibraryDocumentType.ServiceManual,
            MinRole: TelegramUserRole.Engineer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false));

        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-a", "Gree GMV Mini Slim Owner Manual EN 8-16kW A-T C-T C-X.pdf");
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-b", "Gree GMV Mini Slim Owner Manual EN 12-18kW C1-S.pdf");
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-c", "Gree GMV Mini Slim Owner Manual EN 22-35kW H C-X C1-X.pdf");

        var bindings = await bindingStore.ListAsync();
        var owners = bindings
            .Where(binding =>
                binding.IsActive &&
                binding.DocumentType == TelegramLibraryDocumentType.OwnerManual &&
                string.Equals(binding.Series, "GMV Mini", StringComparison.OrdinalIgnoreCase))
            .OrderBy(binding => binding.OriginalFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var service = Assert.Single(bindings, binding =>
            binding.IsActive &&
            binding.DocumentType == TelegramLibraryDocumentType.ServiceManual &&
            string.Equals(binding.Series, "GMV Mini", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(3, owners.Length);
        Assert.All(owners, binding =>
        {
            Assert.StartsWith("gree-gmv-mini-owner-manual-", binding.ManualId, StringComparison.Ordinal);
            Assert.True(binding.CanUseForDiagnostics);
            Assert.Equal(TelegramUserRole.Consumer, binding.MinRole);
        });
        Assert.Equal("telegram-file-id-service", service.TelegramFileId);
        Assert.Equal(4, bindings.Count(binding => string.Equals(binding.Series, "GMV Mini", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task GmvMiniOwnerManualReplaceTargetsSameFileNameOnly()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();
        const string fileA = "Gree GMV Mini Slim Owner Manual EN 8-16kW A-T C-T C-X.pdf";
        const string fileB = "Gree GMV Mini Slim Owner Manual EN 12-18kW C1-S.pdf";
        const string fileC = "Gree GMV Mini Slim Owner Manual EN 22-35kW H C-X C1-X.pdf";

        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-a-old", fileA);
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-b", fileB);
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-c", fileC);

        var replacePrompt = await StartGmvMiniOwnerManualUploadAsync(adapter, "telegram-file-id-owner-a-new", fileA);
        await adapter.HandleAsync(ManualBindCallback("mb:c:cancel"));
        var afterCancel = await GmvMiniOwnerManualsAsync(bindingStore);

        await StartGmvMiniOwnerManualUploadAsync(adapter, "telegram-file-id-owner-a-new", fileA);
        await adapter.HandleAsync(ManualBindCallback("mb:c:replace"));
        var afterReplace = await GmvMiniOwnerManualsAsync(bindingStore);

        Assert.Contains("Заменить", replacePrompt.Text, StringComparison.Ordinal);
        Assert.Equal("telegram-file-id-owner-a-old", Assert.Single(afterCancel, binding => binding.OriginalFileName == fileA).TelegramFileId);
        Assert.Equal("telegram-file-id-owner-a-new", Assert.Single(afterReplace, binding => binding.OriginalFileName == fileA).TelegramFileId);
        Assert.Equal("telegram-file-id-owner-b", Assert.Single(afterReplace, binding => binding.OriginalFileName == fileB).TelegramFileId);
        Assert.Equal("telegram-file-id-owner-c", Assert.Single(afterReplace, binding => binding.OriginalFileName == fileC).TelegramFileId);
        Assert.Equal(3, afterReplace.Length);
    }

    [Fact]
    public async Task GmvMiniOwnerManualLibraryBucketListsAllFilesAndFlexEmptyState()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-a", "Gree GMV Mini Slim Owner Manual EN 8-16kW A-T C-T C-X.pdf");
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-b", "Gree GMV Mini Slim Owner Manual EN 12-18kW C1-S.pdf");
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-c", "Gree GMV Mini Slim Owner Manual EN 22-35kW H C-X C1-X.pdf");

        var bucket = await adapter.HandleAsync(LibraryCallback("lib:gree:outdoor:gmv-mini:owner"));
        var flexEmpty = await adapter.HandleAsync(LibraryCallback("lib:gree:outdoor:gmv9-flex:owner"));
        var buttons = InlineButtons(bucket);

        Assert.Contains(buttons, button => button.Contains("8-16kW", StringComparison.Ordinal));
        Assert.Contains(buttons, button => button.Contains("12-18kW", StringComparison.Ordinal));
        Assert.Contains(buttons, button => button.Contains("22-35kW", StringComparison.Ordinal));
        Assert.Contains("Пока файлов нет", flexEmpty.Text, StringComparison.Ordinal);
        AssertNoDiagnosticSourceLeak(bucket);
        AssertNoDiagnosticSourceLeak(flexEmpty);
    }

    [Fact]
    public async Task DiagnosticGuideSelectsAmongMultipleGmvMiniOwnerManualsOnly()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var bindingStore = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await bindingStore.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv-mini-service-manual",
            "telegram-file-id-service",
            "Gree GMV Mini Slim Service Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "GMV Mini",
            DocumentType: TelegramLibraryDocumentType.ServiceManual,
            MinRole: TelegramUserRole.Engineer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false));
        await bindingStore.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv-mini-installation-manual",
            "telegram-file-id-installation",
            "Gree GMV Mini Slim Installation Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "GMV Mini",
            DocumentType: TelegramLibraryDocumentType.InstallationManual,
            MinRole: TelegramUserRole.Installer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false));

        await adapter.HandleAsync(Update("Gree GMV Mini n2"));
        var serviceOnly = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));

        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-a", "Gree GMV Mini Slim Owner Manual EN 8-16kW A-T C-T C-X.pdf");
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-b", "Gree GMV Mini Slim Owner Manual EN 12-18kW C1-S.pdf");
        await UploadGmvMiniOwnerManualAsync(adapter, "telegram-file-id-owner-c", "Gree GMV Mini Slim Owner Manual EN 22-35kW H C-X C1-X.pdf");
        var owners = await GmvMiniOwnerManualsAsync(bindingStore);
        var selected = Assert.Single(owners, binding => binding.OriginalFileName?.Contains("12-18kW", StringComparison.Ordinal) == true);

        await adapter.HandleAsync(Update("Gree GMV Mini n2"));
        var selection = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));
        var manualButtons = InlineButtonsWithCallbacks(selection)
            .Where(button => button.CallbackData.StartsWith("dm:file:", StringComparison.Ordinal))
            .ToArray();
        var selectedButton = Assert.Single(manualButtons, button => button.Text.Contains("12-18kW", StringComparison.Ordinal));
        var sent = await adapter.HandleAsync(DiagnosticManualFileCallback(selectedButton.CallbackData));
        var flexMissing = await adapter.HandleAsync(Update("Gree GMV9 Flex E0"));
        flexMissing = await adapter.HandleAsync(Update(TelegramManualLibraryService.DiagnosticGuideButton));

        Assert.Contains("<b>Руководство пока не добавлено</b>", serviceOnly.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(serviceOnly.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        Assert.Contains("<b>Выберите руководство</b>", selection.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV Mini Slim Owner Manual EN 8-16kW A-T C-T C-X.pdf", selection.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV Mini Slim Owner Manual EN 12-18kW C1-S.pdf", selection.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV Mini Slim Owner Manual EN 22-35kW H C-X C1-X.pdf", selection.Text, StringComparison.Ordinal);
        Assert.Equal(3, manualButtons.Length);
        Assert.Collection(
            manualButtons,
            button => Assert.Contains("1) 8-16kW A-T C-T C-X", button.Text, StringComparison.Ordinal),
            button => Assert.Contains("2) 12-18kW C1-S", button.Text, StringComparison.Ordinal),
            button => Assert.Contains("3) 22-35kW H C-X C1-X", button.Text, StringComparison.Ordinal));
        Assert.All(manualButtons, button =>
        {
            Assert.True(Encoding.UTF8.GetByteCount(button.CallbackData) <= 64, button.CallbackData);
            Assert.True(Encoding.UTF8.GetByteCount(button.Text) <= 64, button.Text);
            Assert.DoesNotContain("Gree GMV Mini", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(".pdf", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("telegram-file-id", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sourceReferences", button.CallbackData, StringComparison.OrdinalIgnoreCase);
        });
        Assert.DoesNotContain(selected.ManualId, selectedButton.CallbackData, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(InlineButtons(selection), button => button.Contains("8-16kW", StringComparison.Ordinal));
        Assert.Contains(InlineButtons(selection), button => button.Contains("12-18kW", StringComparison.Ordinal));
        Assert.Contains(InlineButtons(selection), button => button.Contains("22-35kW", StringComparison.Ordinal));
        Assert.DoesNotContain(InlineButtons(selection), button => button.Contains("Service Manual", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(InlineButtons(selection), button => button.Contains("Installation Manual", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(selection.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        var document = Assert.Single(sent.OutboundMessages, message => !string.IsNullOrWhiteSpace(message.DocumentFileId));
        Assert.Equal("telegram-file-id-owner-b", document.DocumentFileId);
        Assert.True(document.ProtectContent);
        Assert.Contains("<b>Руководство пока не добавлено</b>", flexMissing.Text, StringComparison.Ordinal);
        AssertNoDiagnosticSourceLeak(selection);
        AssertNoDiagnosticSourceLeak(sent);
    }

    [Fact]
    public async Task RegistrationRejectsUnknownManualId()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(RegisterDocument(
            "/manual_register unknown-manual",
            "telegram-file-id-unknown",
            "manual.pdf"));

        Assert.Contains("не найдено в реестре", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telegram-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrationRejectsRawFileIdTypedByUser()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Owner);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("/manual_register gree-gmv6-service-manual-2020-09 raw-file-id"));

        Assert.Contains("Идентификатор файла не принимается текстом", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImprovedRussianTerminologyDoesNotLeakRawEnglishAbbreviations()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var d1 = await adapter.HandleAsync(Update("Gree GMV6 d1"));
        var c0 = await adapter.HandleAsync(Update("Gree GMV6 C0"));
        var o1 = await adapter.HandleAsync(Update("Gree GMV6 o1"));

        Assert.DoesNotContain("indoor PCB", d1.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("плата управления внутреннего блока", d1.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" IDU", c0.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(" ODU", c0.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(" IDU", o1.Text, StringComparison.Ordinal);
        Assert.Contains("Подтвердите код C0, серию GMV6 и место индикации.", c0.Text, StringComparison.Ordinal);
        Assert.Contains("Сверьте модель, условия появления и сопутствующие коды.", c0.Text, StringComparison.Ordinal);
        Assert.Contains("Подтвердите код o1", o1.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Сверьте модель, условия появления и сопутствующие коды.", o1.Text, StringComparison.Ordinal);
    }

    private static async Task UploadGmvMiniOwnerManualAsync(
        IEquipmentDiagnosticTelegramAdapter adapter,
        string fileId,
        string fileName)
    {
        await StartGmvMiniOwnerManualUploadAsync(adapter, fileId, fileName);
        await adapter.HandleAsync(ManualBindCallback("mb:c:bind"));
    }

    private static async Task<EquipmentDiagnosticTelegramResponse> StartGmvMiniOwnerManualUploadAsync(
        IEquipmentDiagnosticTelegramAdapter adapter,
        string fileId,
        string fileName)
    {
        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:b:gree"));
        await adapter.HandleAsync(ManualBindCallback("mb:sec:outdoor"));
        await adapter.HandleAsync(ManualBindCallback("mb:s:gmv-mini"));
        await adapter.HandleAsync(ManualBindCallback("mb:dt:owner"));
        return await adapter.HandleAsync(DocumentUpdate(fileId, fileName));
    }

    private static async Task<TelegramManualFileBinding[]> GmvMiniOwnerManualsAsync(
        ITelegramManualFileBindingStore bindingStore) =>
        (await bindingStore.ListAsync())
        .Where(binding =>
            binding.IsActive &&
            binding.DocumentType == TelegramLibraryDocumentType.OwnerManual &&
            string.Equals(binding.Brand, "Gree", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(binding.Series, "GMV Mini", StringComparison.OrdinalIgnoreCase))
        .OrderBy(binding => binding.OriginalFileName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static Task SeedIndoorBindingAsync(
        ITelegramManualFileBindingStore bindingStore,
        string key,
        string fileId,
        string fileName,
        TelegramLibraryDocumentType documentType = TelegramLibraryDocumentType.OwnerManual,
        TelegramUserRole minRole = TelegramUserRole.Consumer,
        bool isActive = true,
        bool isLibraryVisible = true) =>
        bindingStore.UpsertAsync(new TelegramManualFileBinding(
            $"gree-indoor-{key}",
            fileId,
            fileName,
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "Indoor",
            IsActive: isActive,
            Title: fileName,
            DocumentType: documentType,
            MinRole: minRole,
            IsLibraryVisible: isLibraryVisible,
            CanUseForDiagnostics: false));

    private static Task SeedControllerBindingAsync(
        ITelegramManualFileBindingStore bindingStore,
        string key,
        string fileId,
        string fileName,
        TelegramLibraryDocumentType documentType = TelegramLibraryDocumentType.ControllerGuide,
        TelegramUserRole minRole = TelegramUserRole.Installer,
        bool isActive = true,
        bool isLibraryVisible = true) =>
        bindingStore.UpsertAsync(new TelegramManualFileBinding(
            $"gree-controllers-{key}",
            fileId,
            fileName,
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            TelegramUserRole.Owner.ToString(),
            Brand: "Gree",
            Series: "Controllers",
            IsActive: isActive,
            Title: fileName,
            DocumentType: documentType,
            MinRole: minRole,
            IsLibraryVisible: isLibraryVisible,
            CanUseForDiagnostics: false));

    private static ServiceProvider CreateProvider(
        string? bindingPath = null,
        FakeOutbound? outbound = null)
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        if (outbound is not null)
        {
            services.AddSingleton<IEquipmentDiagnosticTelegramOutboundClient>(outbound);
        }

        services.AddSingleton(Options(bindingPath));
        return services.BuildServiceProvider();
    }

    private static async Task AllowAsync(
        ServiceProvider provider,
        TelegramUserRole role)
    {
        var store = provider.GetRequiredService<ITelegramUserStore>();
        await store.AllowAsync(7, role);
    }

    private static async Task SetRoleAndGrantLibraryAsync(
        ServiceProvider provider,
        TelegramUserRole role)
    {
        var store = provider.GetRequiredService<ITelegramUserStore>();
        var accessStore = provider.GetRequiredService<ITelegramLibraryAccessStore>();
        await store.SetRoleAsync(7, role);
        var user = await store.GetByChatIdAsync(7);
        Assert.NotNull(user);
        await accessStore.GrantAsync(user.Id, user.Id);
    }

    private static async Task CreateUserAsync(
        ServiceProvider provider,
        long chatId,
        long userId,
        string? username,
        string? firstName,
        string? lastName,
        TelegramUserRole role)
    {
        var store = provider.GetRequiredService<ITelegramUserStore>();
        await store.GetOrCreateConsumerAsync(Update("/start", chatId, userId, username, firstName, lastName));
        await store.SetRoleAsync(chatId, role);
    }

    private static EquipmentDiagnosticTelegramOptions Options(string? bindingPath) => new()
    {
        IsEnabled = true,
        DefaultManufacturer = "Gree",
        MaxMessageLength = 900,
        ManualLibrary = new TelegramManualLibraryOptions
        {
            FileBindingsPath = bindingPath ?? TempBindingPath()
        }
    };

    private static string TempBindingPath() =>
        Path.Combine(Path.GetTempPath(), $"assistant-engineer-manual-bindings-{Guid.NewGuid():N}.json");

    private static EquipmentDiagnosticTelegramUpdate Update(
        string text,
        long chatId = 7,
        long? userId = 11,
        string? username = "operator",
        string? firstName = null,
        string? lastName = null) =>
        new(UpdateId: 1, ChatId: chatId, Username: username, Text: text, UserId: userId, FirstName: firstName, LastName: lastName);

    private static EquipmentDiagnosticTelegramUpdate RegisterDocument(
        string caption,
        string fileId,
        string fileName) =>
        new(
            UpdateId: 2,
            ChatId: 7,
            Username: "operator",
            Text: caption,
            UserId: 11,
            DocumentFileId: fileId,
            DocumentFileName: fileName,
            DocumentMimeType: "application/pdf");

    private static EquipmentDiagnosticTelegramUpdate DocumentUpdate(
        string fileId,
        string fileName,
        string? fileUniqueId = null,
        long? fileSize = null) =>
        new(
            UpdateId: 4,
            ChatId: 7,
            Username: "operator",
            Text: null,
            UserId: 11,
            DocumentFileId: fileId,
            DocumentFileName: fileName,
            DocumentMimeType: "application/pdf",
            DocumentFileSize: fileSize,
            DocumentFileUniqueId: fileUniqueId);

    private static EquipmentDiagnosticTelegramUpdate ManualCallback() =>
        new(
            UpdateId: 3,
            ChatId: 7,
            Username: "operator",
            Text: null,
            UserId: 11,
            CallbackQueryId: "callback-query-id",
            CallbackData: TelegramManualLibraryService.DiagnosticManualCallbackData);

    private static EquipmentDiagnosticTelegramUpdate DiagnosticManualFileCallback(string callbackData) =>
        new(
            UpdateId: 33,
            ChatId: 7,
            Username: "operator",
            Text: null,
            UserId: 11,
            CallbackQueryId: "diagnostic-manual-file-callback-query-id",
            CallbackData: callbackData);

    private static EquipmentDiagnosticTelegramUpdate ManualBindCallback(string callbackData) =>
        new(
            UpdateId: 5,
            ChatId: 7,
            Username: "operator",
            Text: null,
            UserId: 11,
            CallbackQueryId: "manual-bind-callback-query-id",
            CallbackData: callbackData);

    private static EquipmentDiagnosticTelegramUpdate LibraryCallback(
        string callbackData,
        long chatId = 7,
        long? userId = 11,
        string? username = "operator",
        string? firstName = null,
        string? lastName = null,
        long? messageId = 99) =>
        new(
            UpdateId: 6,
            ChatId: chatId,
            Username: username,
            Text: null,
            MessageId: messageId,
            UserId: userId,
            FirstName: firstName,
            LastName: lastName,
            CallbackQueryId: "library-callback-query-id",
            CallbackData: callbackData);

    private static bool HasButton(
        EquipmentDiagnosticTelegramResponse response,
        string text) =>
        Buttons(response)
            .Any(button => string.Equals(button, text, StringComparison.Ordinal));

    private static string[] Buttons(EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages
            .SelectMany(message => message.ReplyMarkup?.Keyboard ?? [])
            .SelectMany(row => row)
            .Select(button => button.Text)
            .ToArray();

    private static string[][] KeyboardRows(EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages
            .SelectMany(message => message.ReplyMarkup?.Keyboard ?? [])
            .Select(row => row.Select(button => button.Text).ToArray())
            .ToArray();

    private static string[] InlineButtons(EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages
            .SelectMany(message => message.ReplyMarkup?.InlineKeyboard ?? [])
            .SelectMany(row => row)
            .Select(button => button.Text)
            .ToArray();

    private static (string Text, string CallbackData)[] InlineButtonsWithCallbacks(EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages
            .SelectMany(message => message.ReplyMarkup?.InlineKeyboard ?? [])
            .SelectMany(row => row)
            .Select(button => (button.Text, button.CallbackData))
            .ToArray();

    private static void AssertSafeLibraryFileButtons(EquipmentDiagnosticTelegramResponse response)
    {
        var fileButtons = InlineButtonsWithCallbacks(response)
            .Where(button => button.CallbackData.StartsWith("lib:f:", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(fileButtons);
        Assert.All(fileButtons, button =>
        {
            Assert.True(Encoding.UTF8.GetByteCount(button.CallbackData) <= 64, button.CallbackData);
            Assert.True(Encoding.UTF8.GetByteCount(button.Text) <= 64, button.Text);
            Assert.DoesNotContain(".pdf", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("telegram-file-id", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("file_unique_id", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sourceReferences", button.CallbackData, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree", button.CallbackData, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static int TextSendCount(EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages.Count(message =>
            string.IsNullOrWhiteSpace(message.DocumentFileId) &&
            message.EditMessageId is null);

    private static int EditTextCount(EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages.Count(message =>
            string.IsNullOrWhiteSpace(message.DocumentFileId) &&
            message.EditMessageId is not null);

    private static void AssertSingleEdit(
        EquipmentDiagnosticTelegramResponse response,
        long messageId)
    {
        var edit = Assert.Single(response.OutboundMessages);
        Assert.Null(edit.DocumentFileId);
        Assert.Equal(messageId, edit.EditMessageId);
    }

    private static string[] KeyboardButtons(EquipmentDiagnosticTelegramReplyMarkup? replyMarkup) =>
        (replyMarkup?.Keyboard ?? [])
            .SelectMany(row => row)
            .Select(button => button.Text)
            .ToArray();

    private static void AssertNotStale(EquipmentDiagnosticTelegramResponse response)
    {
        Assert.DoesNotContain("Действие библиотеки устарело", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Действие устарело", response.CallbackAnswerText ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoDiagnosticSourceLeak(EquipmentDiagnosticTelegramResponse response)
    {
        foreach (var message in response.OutboundMessages)
        {
            AssertNoSensitiveManualLeak(message.Text);
            Assert.DoesNotContain("GC202512-I", message.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("GC202209-I", message.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("GC202203-IV", message.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sourceReferences", message.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sourceMeaning", message.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("chat_id", message.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("message_id", message.Text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertNoSensitiveManualLeak(string text)
    {
        Assert.DoesNotContain("telegram-file-id", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manualId", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("packageId", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("file_id", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("artifacts/manual-intake", text, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
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
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent."));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Synced."));
    }
}
