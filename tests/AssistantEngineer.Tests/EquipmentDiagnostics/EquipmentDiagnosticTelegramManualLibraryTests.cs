using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
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

        await adapter.HandleAsync(Update("Gree d1"));
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
        await adapter.HandleAsync(Update("Gree d1"));

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
        await adapter.HandleAsync(Update("Gree d1"));

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
        var selected = await adapter.HandleAsync(ManualBindCallback("mb:s:gmv9-flex"));
        var nonDocument = await adapter.HandleAsync(Update("hello"));
        var badName = await adapter.HandleAsync(DocumentUpdate("telegram-file-id-bad", "Gree Manual.txt"));

        Assert.Contains("Gree GMV9 Flex", InlineButtons(start), StringComparer.Ordinal);
        Assert.Contains("Gree GMV9 Flex Service Manual EN Rev B.pdf", selected.Text, StringComparison.Ordinal);
        Assert.Contains("Ожидаю PDF-файл мануала", nonDocument.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV9 Flex Service Manual EN Rev B.pdf", badName.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-file-id-bad", badName.Text, StringComparison.OrdinalIgnoreCase);
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
        await adapter.HandleAsync(ManualBindCallback("mb:s:gmv9-flex"));
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

        Assert.Contains("Подтвердите привязку", candidate.Text, StringComparison.Ordinal);
        Assert.Contains("Мануал привязан", confirmed.Text, StringComparison.Ordinal);
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
        Assert.Contains(
            libraryFile.OutboundMessages,
            message => message.DocumentFileId == "telegram-file-id-gmv9-flex" && message.ProtectContent);
        Assert.DoesNotContain("forward", manual.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("copy", manual.Text, StringComparison.OrdinalIgnoreCase);
        AssertNoDiagnosticSourceLeak(manual);
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
        await adapter.HandleAsync(ManualBindCallback("mb:s:gmv6"));
        var replacePrompt = await adapter.HandleAsync(DocumentUpdate("telegram-file-id-new", "Gree GMV6 Service Manual EN Rev C.pdf"));
        await adapter.HandleAsync(ManualBindCallback("mb:c:cancel"));
        var afterCancel = await bindingStore.GetBySeriesAsync("Gree", "GMV6");

        await adapter.HandleAsync(Update("/manual_bind"));
        await adapter.HandleAsync(ManualBindCallback("mb:s:gmv6"));
        await adapter.HandleAsync(DocumentUpdate("telegram-file-id-new", "Gree GMV6 Service Manual EN Rev C.pdf"));
        await adapter.HandleAsync(ManualBindCallback("mb:c:replace"));
        var afterReplace = await bindingStore.GetBySeriesAsync("Gree", "GMV6");

        Assert.Contains("Заменить", replacePrompt.Text, StringComparison.Ordinal);
        Assert.Equal("telegram-file-id-old", afterCancel?.TelegramFileId);
        Assert.Equal("telegram-file-id-new", afterReplace?.TelegramFileId);
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

        var d1 = await adapter.HandleAsync(Update("Gree d1"));
        var c0 = await adapter.HandleAsync(Update("Gree C0"));
        var o1 = await adapter.HandleAsync(Update("Gree o1"));

        Assert.DoesNotContain("indoor PCB", d1.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("плата управления внутреннего блока", d1.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" IDU", c0.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(" ODU", c0.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(" IDU", o1.Text, StringComparison.Ordinal);
        Assert.Contains("Подтвердите код C0, серию GMV и место индикации.", c0.Text, StringComparison.Ordinal);
        Assert.Contains("Сверьте модель, условия появления и сопутствующие коды.", c0.Text, StringComparison.Ordinal);
        Assert.Contains("Подтвердите код o1", o1.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Сверьте модель, условия появления и сопутствующие коды.", o1.Text, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateProvider(string? bindingPath = null)
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
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

    private static EquipmentDiagnosticTelegramUpdate Update(string text) =>
        new(UpdateId: 1, ChatId: 7, Username: "operator", Text: text, UserId: 11);

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

    private static EquipmentDiagnosticTelegramUpdate ManualBindCallback(string callbackData) =>
        new(
            UpdateId: 5,
            ChatId: 7,
            Username: "operator",
            Text: null,
            UserId: 11,
            CallbackQueryId: "manual-bind-callback-query-id",
            CallbackData: callbackData);

    private static EquipmentDiagnosticTelegramUpdate LibraryCallback(string callbackData) =>
        new(
            UpdateId: 6,
            ChatId: 7,
            Username: "operator",
            Text: null,
            UserId: 11,
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
}
