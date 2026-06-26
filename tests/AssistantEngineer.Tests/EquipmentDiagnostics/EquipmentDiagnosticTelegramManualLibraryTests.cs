using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramManualLibraryTests
{
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
        var response = await adapter.HandleAsync(Update(TelegramManualLibraryService.ManualLibraryButton));

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
        await AllowAsync(provider, TelegramUserRole.Admin);
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
        await AllowAsync(provider, TelegramUserRole.Admin);
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

    [Fact]
    public async Task NonAdminCannotRegisterManualFile()
    {
        using var provider = CreateProvider();
        await AllowAsync(provider, TelegramUserRole.Engineer);
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(RegisterDocument(
            "/manual_register gree-gmv6-service-manual-2020-09",
            "telegram-file-id-gmv6",
            "Service Manual for GMV6 v_2020.09.pdf"));

        Assert.Contains("только админу или владельцу", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telegram-file-id", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Owner)]
    public async Task AdminOrOwnerCanRegisterManualFile(TelegramUserRole role)
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
    public async Task NonAdminRolesCannotManageManualBindings(TelegramUserRole role)
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
    public async Task AdminCanUnregisterManualFile()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Admin);
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
        await AllowAsync(provider, TelegramUserRole.Admin);
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

    [Fact]
    public async Task RegistrationRejectsUnknownManualId()
    {
        using var provider = CreateProvider(TempBindingPath());
        await AllowAsync(provider, TelegramUserRole.Admin);
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
        await AllowAsync(provider, TelegramUserRole.Admin);
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
        Assert.Contains("внутреннего блока", c0.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("наружного блока", c0.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("шины внутреннего блока", o1.Text, StringComparison.OrdinalIgnoreCase);
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
