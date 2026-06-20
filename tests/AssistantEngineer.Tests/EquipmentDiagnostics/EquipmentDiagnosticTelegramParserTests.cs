using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramParserTests
{
    private readonly EquipmentDiagnosticTelegramMessageParser _parser = new();

    [Theory]
    [InlineData("Gree H5", "Gree", "H5")]
    [InlineData("/diagnose Gree H5", "Gree", "H5")]
    [InlineData("H5", "Gree", "H5")]
    [InlineData("Gree A0", "Gree", "A0")]
    [InlineData("Gree U0", "Gree", "U0")]
    [InlineData("Gree GMV6 U0", "Gree", "U0")]
    [InlineData("Gree debugging U0", "Gree", "U0")]
    [InlineData("n6", "Gree", "n6")]
    [InlineData("db", "Gree", "db")]
    public void DiagnosticMessagesParseDeterministically(string text, string manufacturer, string code)
    {
        var result = _parser.Parse(text, EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal(EquipmentDiagnosticTelegramCommand.Diagnose, result.Command);
        Assert.Equal(manufacturer, result.DiagnosticRequest!.Manufacturer);
        Assert.Equal(code, result.DiagnosticRequest.Code);
    }

    [Fact]
    public void Gmv6SeriesHintIsPreserved()
    {
        var result = _parser.Parse("Gree GMV6 U0", EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal("GMV6", result.DiagnosticRequest!.Series);
    }

    [Theory]
    [InlineData("Gree C5 outdoor", EquipmentDiagnosticBotEquipmentSide.Outdoor)]
    [InlineData("Gree F5 ODU", EquipmentDiagnosticBotEquipmentSide.Outdoor)]
    [InlineData("Gree C5 indoor", EquipmentDiagnosticBotEquipmentSide.Indoor)]
    [InlineData("Gree E6 chiller", EquipmentDiagnosticBotEquipmentSide.Chiller)]
    public void EquipmentSideHintsAreRecognized(
        string text,
        EquipmentDiagnosticBotEquipmentSide expectedSide)
    {
        var result = _parser.Parse(text, EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal(expectedSide, result.DiagnosticRequest!.EquipmentSide);
    }

    [Theory]
    [InlineData("Gree E1 led", EquipmentDiagnosticBotDisplayContext.OduMainBoardLed)]
    [InlineData("Gree E1 wired controller", EquipmentDiagnosticBotDisplayContext.WiredController)]
    [InlineData("Gree E1 gateway", EquipmentDiagnosticBotDisplayContext.MobileAppOrGateway)]
    public void DisplayContextHintsAreRecognized(
        string text,
        EquipmentDiagnosticBotDisplayContext expectedContext)
    {
        var result = _parser.Parse(text, EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal(expectedContext, result.DiagnosticRequest!.DisplayContext);
    }

    [Theory]
    [InlineData("/start", EquipmentDiagnosticTelegramCommand.Start)]
    [InlineData("/help", EquipmentDiagnosticTelegramCommand.Help)]
    [InlineData("/history", EquipmentDiagnosticTelegramCommand.History)]
    [InlineData("/last", EquipmentDiagnosticTelegramCommand.Last)]
    [InlineData("/id", EquipmentDiagnosticTelegramCommand.Identity)]
    [InlineData("/WHOAMI", EquipmentDiagnosticTelegramCommand.Identity)]
    public void HelpCommandsDoNotCreateDiagnosticRequest(
        string text,
        EquipmentDiagnosticTelegramCommand expectedCommand)
    {
        var result = _parser.Parse(text, EnabledOptions());

        Assert.Equal(expectedCommand, result.Command);
        Assert.Null(result.DiagnosticRequest);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("CE41")]
    [InlineData("CE42")]
    [InlineData("CE52")]
    public void ControllerModelNamesAreUnsupportedRatherThanFaultCodes(string text)
    {
        var result = _parser.Parse(text, EnabledOptions());

        Assert.Equal(EquipmentDiagnosticTelegramCommand.Unsupported, result.Command);
        Assert.Null(result.DiagnosticRequest);
    }

    [Fact]
    public void MissingManufacturerWithoutDefaultIsRejected()
    {
        var options = EnabledOptions() with
        {
            DefaultManufacturer = null,
            RequireExplicitManufacturer = true
        };

        var result = _parser.Parse("H5", options);

        Assert.Null(result.DiagnosticRequest);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void TooLongMessageIsRejected()
    {
        var result = _parser.Parse(new string('A', 101), EnabledOptions() with { MaxMessageLength = 100 });

        Assert.Null(result.DiagnosticRequest);
        Assert.Contains(result.Errors, error => error.Contains("at most 100", StringComparison.Ordinal));
    }

    [Fact]
    public void ControlCharactersAreRejected()
    {
        var result = _parser.Parse("Gree\u0000 H5", EnabledOptions());

        Assert.Null(result.DiagnosticRequest);
        Assert.Contains(result.Errors, error => error.Contains("control", StringComparison.OrdinalIgnoreCase));
    }

    private static EquipmentDiagnosticTelegramOptions EnabledOptions() => new()
    {
        IsEnabled = true,
        DefaultManufacturer = "Gree"
    };
}
