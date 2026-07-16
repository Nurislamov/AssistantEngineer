using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramParserTests
{
    private readonly EquipmentDiagnosticTelegramMessageParser _parser = new();

    [Theory]
    [InlineData("Gree H5", "Gree", "H5")]
    [InlineData("/diagnose Gree H5", "Gree", "H5")]
    [InlineData("/diagnose А 1", "Gree", "A1")]
    [InlineData("H5", "Gree", "H5")]
    [InlineData("Gree A0", "Gree", "A0")]
    [InlineData("Gree U0", "Gree", "U0")]
    [InlineData("Gree GMV6 U0", "Gree", "U0")]
    [InlineData("Gree GMV Mini AJ", "Gree", "AJ")]
    [InlineData("Gree GMV Mini C0", "Gree", "C0")]
    [InlineData("GMV Mini AJ", "Gree", "AJ")]
    [InlineData("GMV Mini C0", "Gree", "C0")]
    [InlineData("Gree debugging U0", "Gree", "U0")]
    [InlineData("Gree 01", "Gree", "01")]
    [InlineData("Gree А1", "Gree", "A1")]
    [InlineData("Gree а1", "Gree", "a1")]
    [InlineData("А1", "Gree", "A1")]
    [InlineData("а1", "Gree", "a1")]
    [InlineData("Л3", "Gree", "L3")]
    [InlineData("л3", "Gree", "l3")]
    [InlineData("Н5", "Gree", "H5")]
    [InlineData("с0", "Gree", "c0")]
    [InlineData("л 3", "Gree", "l3")]
    [InlineData("А 1", "Gree", "A1")]
    [InlineData("Gree А 1", "Gree", "A1")]
    [InlineData("Gree GMV6 А 1", "Gree", "A1")]
    [InlineData("GMV6 а 1", "Gree", "a1")]
    [InlineData("Gree л 3", "Gree", "l3")]
    [InlineData("ошибка А 1", "ошибка", "A1")]
    [InlineData("ошибка: а 1.", "ошибка", "a1")]
    [InlineData("ошибка: А 1", "ошибка", "A1")]
    [InlineData("код а 1", "код", "a1")]
    [InlineData("код л 3", "код", "l3")]
    [InlineData("код: А 1", "код", "A1")]
    [InlineData("код: л 3", "код", "l3")]
    [InlineData("code A 1", "code", "A1")]
    [InlineData("показывает Н 5", "показывает", "H5")]
    [InlineData("error A 1", "error", "A1")]
    [InlineData("error l 3", "error", "l3")]
    [InlineData("fault A 1", "fault", "A1")]
    [InlineData("fault l 3", "fault", "l3")]
    [InlineData("на пульте Н 5", "на", "H5")]
    [InlineData("на пульте л 3!", "на", "l3")]
    [InlineData("на пульте код Н 5", "на", "H5")]
    [InlineData("на контроллере Н 5", "на", "H5")]
    [InlineData("контроллер показывает Н 5", "контроллер", "H5")]
    [InlineData("контроллер показывает c 0", "контроллер", "c0")]
    [InlineData("А-1", "Gree", "A1")]
    [InlineData("Н-5", "Gree", "H5")]
    [InlineData("с.0", "Gree", "c0")]
    [InlineData("с_0", "Gree", "c0")]
    [InlineData("О1", "Gree", "O1")]
    [InlineData("о1", "Gree", "o1")]
    [InlineData("01", "Gree", "01")]
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
    public void SpaceSeparatedCodeWithSeriesContextPreservesSeries()
    {
        var result = _parser.Parse("GMV6 А 1", EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal("A1", result.DiagnosticRequest!.Code);
        Assert.Equal("GMV6", result.DiagnosticRequest.Series);
    }

    [Theory]
    [InlineData("А", "A")]
    [InlineData("а", "a")]
    [InlineData("В", "B")]
    [InlineData("в", "b")]
    [InlineData("С", "C")]
    [InlineData("с", "c")]
    [InlineData("Е", "E")]
    [InlineData("е", "e")]
    [InlineData("Н", "H")]
    [InlineData("н", "h")]
    [InlineData("К", "K")]
    [InlineData("к", "k")]
    [InlineData("М", "M")]
    [InlineData("м", "m")]
    [InlineData("О", "O")]
    [InlineData("о", "o")]
    [InlineData("Р", "P")]
    [InlineData("р", "p")]
    [InlineData("Т", "T")]
    [InlineData("т", "t")]
    [InlineData("Х", "X")]
    [InlineData("х", "x")]
    [InlineData("У", "Y")]
    [InlineData("у", "y")]
    [InlineData("Л", "L")]
    [InlineData("л", "l")]
    public void CyrillicVisualMappingsPreserveCase(
        string cyrillic,
        string latin)
    {
        var result = _parser.Parse($"{cyrillic}1", EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal($"{latin}1", result.DiagnosticRequest!.Code);
    }

    [Theory]
    [InlineData("л 3", "l3")]
    [InlineData("ошибка А 1", "A1")]
    [InlineData("ошибка: а 1.", "a1")]
    [InlineData("ошибка: А 1", "A1")]
    [InlineData("код а 1", "a1")]
    [InlineData("код л 3", "l3")]
    [InlineData("код: А 1", "A1")]
    [InlineData("код: л 3", "l3")]
    [InlineData("code A 1", "A1")]
    [InlineData("показывает Н 5", "H5")]
    [InlineData("error A 1", "A1")]
    [InlineData("fault l 3", "l3")]
    [InlineData("на пульте н-5", "h5")]
    [InlineData("на пульте Н 5", "H5")]
    [InlineData("на пульте л 3!", "l3")]
    [InlineData("на пульте код Н 5", "H5")]
    [InlineData("на контроллере Н 5", "H5")]
    [InlineData("контроллер показывает Н 5", "H5")]
    [InlineData("контроллер показывает c 0", "c0")]
    public void TryExtractDiagnosticCodeNormalizesCyrillicAndSeparatedInput(
        string text,
        string expectedCode)
    {
        var extracted = _parser.TryExtractDiagnosticCode(text, out var code);

        Assert.True(extracted);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("у меня 13 блоков")]
    [InlineData("блок 1 3 этаж")]
    [InlineData("телефон +998 90 123 45 67")]
    [InlineData("я буду в 3 часа")]
    [InlineData("работает с 0 до 5")]
    [InlineData("подключить к 1 блоку")]
    [InlineData("а 1 блок исправен")]
    [InlineData("говорили о 1 системе")]
    [InlineData("в 3 помещении")]
    [InlineData("на 1 этаже")]
    [InlineData("температура с 0 поднялась до 5")]
    [InlineData("к 1 наружному подключено 5 внутренних")]
    [InlineData("ошибка была в 3 часа")]
    [InlineData("ошибка в 3 часа")]
    [InlineData("ошибка: в 3 часа")]
    [InlineData("ошибка пропала, буду в 3")]
    [InlineData("контроллер работает с 0 до 5")]
    [InlineData("пульт включится в 3 часа")]
    [InlineData("показывает в 3 часа")]
    [InlineData("показывает температуру с 0 градусов")]
    [InlineData("ошибка с 0 до 5")]
    [InlineData("ошибка: с 0 до 5")]
    [InlineData("на контроллере температура с 0 поднялась до 5")]
    [InlineData("контроллер показывает в 3 помещении")]
    [InlineData("контроллер показывает с 0 до 5")]
    [InlineData("на пульте с 0 до 5")]
    [InlineData("на контроллере в 3 часа будет проверка")]
    [InlineData("ошибка была вчера, а 1 блок сегодня работает")]
    [InlineData("код записан в документе, а 1 блок отключён")]
    [InlineData("пульт исправен, в 3 часа будет проверка")]
    [InlineData("контроллер подключён к 1 наружному блоку")]
    [InlineData("Gree работает с 0 до 5")]
    [InlineData("Gree подключён к 1 наружному блоку")]
    [InlineData("Gree будет в 3 часа")]
    [InlineData("GMV6 будет в 3 часа")]
    [InlineData("GMV6 работает с 0 до 5")]
    [InlineData("GMV6 подключён к 1 блоку")]
    [InlineData("код в 3 строке документа")]
    [InlineData("код: в 3 строке")]
    [InlineData("код с 0 до 5")]
    [InlineData("code at 3 pm")]
    [InlineData("/diagnose в 3 часа")]
    [InlineData("/diagnose с 0 до 5")]
    [InlineData("fault occurred yesterday, I will arrive at 3")]
    [InlineData("fault at 3 o'clock")]
    [InlineData("error at 3 pm")]
    [InlineData("/help")]
    public void TryExtractDiagnosticCodeDoesNotInventCodesFromOrdinaryMessages(string text)
    {
        Assert.False(_parser.TryExtractDiagnosticCode(text, out _));
    }

    [Theory]
    [InlineData("у меня 13 блоков")]
    [InlineData("блок 1 3 этаж")]
    [InlineData("телефон +998 90 123 45 67")]
    [InlineData("я буду в 3 часа")]
    [InlineData("работает с 0 до 5")]
    [InlineData("подключить к 1 блоку")]
    [InlineData("а 1 блок исправен")]
    [InlineData("говорили о 1 системе")]
    [InlineData("в 3 помещении")]
    [InlineData("на 1 этаже")]
    [InlineData("температура с 0 поднялась до 5")]
    [InlineData("к 1 наружному подключено 5 внутренних")]
    [InlineData("ошибка была в 3 часа")]
    [InlineData("ошибка в 3 часа")]
    [InlineData("ошибка: в 3 часа")]
    [InlineData("ошибка пропала, буду в 3")]
    [InlineData("контроллер работает с 0 до 5")]
    [InlineData("пульт включится в 3 часа")]
    [InlineData("показывает в 3 часа")]
    [InlineData("показывает температуру с 0 градусов")]
    [InlineData("ошибка с 0 до 5")]
    [InlineData("ошибка: с 0 до 5")]
    [InlineData("на контроллере температура с 0 поднялась до 5")]
    [InlineData("контроллер показывает в 3 помещении")]
    [InlineData("контроллер показывает с 0 до 5")]
    [InlineData("на пульте с 0 до 5")]
    [InlineData("на контроллере в 3 часа будет проверка")]
    [InlineData("ошибка была вчера, а 1 блок сегодня работает")]
    [InlineData("код записан в документе, а 1 блок отключён")]
    [InlineData("пульт исправен, в 3 часа будет проверка")]
    [InlineData("контроллер подключён к 1 наружному блоку")]
    [InlineData("Gree работает с 0 до 5")]
    [InlineData("Gree подключён к 1 наружному блоку")]
    [InlineData("Gree будет в 3 часа")]
    [InlineData("GMV6 будет в 3 часа")]
    [InlineData("GMV6 работает с 0 до 5")]
    [InlineData("GMV6 подключён к 1 блоку")]
    [InlineData("код в 3 строке документа")]
    [InlineData("код: в 3 строке")]
    [InlineData("код с 0 до 5")]
    [InlineData("code at 3 pm")]
    [InlineData("/diagnose в 3 часа")]
    [InlineData("/diagnose с 0 до 5")]
    [InlineData("fault occurred yesterday, I will arrive at 3")]
    [InlineData("fault at 3 o'clock")]
    [InlineData("error at 3 pm")]
    public void OrdinaryMessagesDoNotParseAsDiagnosticRequests(string text)
    {
        var result = _parser.Parse(text, EnabledOptions());

        Assert.Equal(EquipmentDiagnosticTelegramCommand.Diagnose, result.Command);
        Assert.Null(result.DiagnosticRequest);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Gmv6SeriesHintIsPreserved()
    {
        var result = _parser.Parse("Gree GMV6 U0", EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal("GMV6", result.DiagnosticRequest!.Series);
    }

    [Theory]
    [InlineData("Gree GMV Mini AJ")]
    [InlineData("Gree GMV-Mini AJ")]
    [InlineData("Gree GMV5 Mini AJ")]
    [InlineData("GMV Mini AJ")]
    public void GmvMiniSeriesHintIsPreserved(string text)
    {
        var result = _parser.Parse(text, EnabledOptions());

        Assert.Empty(result.Errors);
        Assert.Equal("GMV Mini", result.DiagnosticRequest!.Series);
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
        Assert.False(_parser.TryExtractDiagnosticCode(text, out _));
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
