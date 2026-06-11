using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramFormatterTests
{
    [Theory]
    [InlineData("H5", EquipmentDiagnosticBotResponseStatus.Answer, "Confidence:")]
    [InlineData("E1", EquipmentDiagnosticBotResponseStatus.ClarificationRequired, "reply with")]
    [InlineData("A0", EquipmentDiagnosticBotResponseStatus.ReferenceOnly, "Reference-only")]
    [InlineData("ZZ99", EquipmentDiagnosticBotResponseStatus.NotFound, "Verify the manufacturer")]
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
        Assert.Contains("Safety:", first, StringComparison.Ordinal);
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
        Assert.Contains("Response shortened", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelpAndValidationArePlainTextAndLengthBounded()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();

        var help = formatter.FormatHelp(500);
        var validation = formatter.FormatValidation(["Manufacturer is required."], 500);

        Assert.Contains("Gree H5", help, StringComparison.Ordinal);
        Assert.Contains("Manufacturer is required", validation, StringComparison.Ordinal);
        Assert.True(help.Length <= 500);
        Assert.True(validation.Length <= 500);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }
}
