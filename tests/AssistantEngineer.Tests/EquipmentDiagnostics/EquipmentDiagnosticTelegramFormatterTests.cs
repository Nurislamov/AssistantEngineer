using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramFormatterTests
{
    [Theory]
    [InlineData("H5", EquipmentDiagnosticBotResponseStatus.Answer, "Уверенность:")]
    [InlineData("E1", EquipmentDiagnosticBotResponseStatus.ClarificationRequired, "укажите контекст")]
    [InlineData("A0", EquipmentDiagnosticBotResponseStatus.ReferenceOnly, "Gree GMV6 A0")]
    [InlineData("ZZ99", EquipmentDiagnosticBotResponseStatus.NotFound, "Код не найден")]
    public async Task StatusFormatsAreDeterministic(string code, EquipmentDiagnosticBotResponseStatus status, string expected)
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", code));

        var first = formatter.Format(response, 900);
        var second = formatter.Format(response, 900);

        Assert.Equal(status, response.Status);
        Assert.Equal(first, second);
        Assert.Contains(expected, first, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Безопасность:", first, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormatterAppliesDeterministicLengthLimit()
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5"));

        var text = formatter.Format(response, 220);

        Assert.True(text.Length <= 220);
        Assert.Contains("Ответ сокращен", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelpAndValidationArePlainTextAndLengthBounded()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();

        var help = formatter.FormatHelp(500);
        var validation = formatter.FormatValidation(["Manufacturer is required."], 500);

        Assert.Contains("Gree H5", help, StringComparison.Ordinal);
        Assert.Contains("Укажите производителя", validation, StringComparison.Ordinal);
        Assert.True(help.Length <= 500);
        Assert.True(validation.Length <= 500);
    }

    [Fact]
    public async Task ConsumerFormatIsRussianAndDoesNotExposeTechnicalMarkers()
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5"));

        var text = formatter.FormatConsumer(response, hasPhoneNumber: false, maxLength: 900);

        Assert.Contains("Возможное значение", text, StringComparison.Ordinal);
        Assert.Contains("Что можно сделать безопасно", text, StringComparison.Ordinal);
        Assert.Contains("Для сервиса", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Confidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Source", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deterministic bot API", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Response shortened", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Preliminary diagnostic", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Safe next steps", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal trace", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Installer)]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Owner)]
    public async Task GreeH5TechnicalOutputIsRussianForTechnicalRoles(TelegramUserRole role)
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = provider.GetRequiredService<EquipmentDiagnosticTelegramResponseFormatter>();
        var response = await facade.DiagnoseAsync(
            new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"));

        var text = formatter.FormatTechnical(response, role);

        Assert.Contains("Диагностика GREE H5", text, StringComparison.Ordinal);
        Assert.Contains("Кратко:", text, StringComparison.Ordinal);
        Assert.Contains("Безопасность:", text, StringComparison.Ordinal);
        Assert.Contains("Возможные причины:", text, StringComparison.Ordinal);
        Assert.Contains("Что проверить:", text, StringComparison.Ordinal);
        Assert.Contains("Что не советовать клиенту:", text, StringComparison.Ordinal);
        Assert.Contains("Рекомендованное действие:", text, StringComparison.Ordinal);
        Assert.Contains("Уверенность: Высокая", text, StringComparison.Ordinal);
        Assert.Contains("Источник: руководство производителя", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Черновик / непроверено", text, StringComparison.Ordinal);
        foreach (var forbidden in EnglishTechnicalMarkers())
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<string> EnglishTechnicalMarkers() =>
    [
        "Safety",
        "Step ",
        "Source:",
        "Confidence:",
        " Low",
        " Medium",
        " High",
        "Preliminary diagnostic entry",
        "Recommended action"
    ];

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }
}
