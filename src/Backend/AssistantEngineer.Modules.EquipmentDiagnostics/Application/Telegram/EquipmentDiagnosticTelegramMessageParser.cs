using System.Text.RegularExpressions;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot.Routing;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed partial class EquipmentDiagnosticTelegramMessageParser
{
    private static readonly string[] OutdoorHints = ["outdoor", "odu", "наружный", "наружка"];
    private static readonly string[] IndoorHints = ["indoor", "idu", "внутренний", "внутрянка"];
    private static readonly string[] ChillerHints = ["chiller", "чиллер"];
    private static readonly string[] ControllerHints = ["controller", "пульт", "контроллер"];
    private static readonly string[] KnowledgeHints = ["debugging", "commissioning", "status", "наладка", "статус"];
    private static readonly string[] LedHints = ["led", "board", "плата"];
    private static readonly string[] WiredControllerHints = ["wired controller", "пульт"];
    private static readonly string[] GatewayHints = ["app", "gateway", "шлюз"];
    private static readonly string[] ControllerModelNames = ["CE41", "CE42", "CE52"];

    public EquipmentDiagnosticTelegramParseResult Parse(
        string? text,
        EquipmentDiagnosticTelegramOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Invalid("Message text is required.");
        }

        if (text.Length > options.MaxMessageLength)
        {
            return Invalid($"Message must contain at most {options.MaxMessageLength} characters.");
        }

        if (text.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t'))
        {
            return Invalid("Message contains unsupported control characters.");
        }

        var trimmed = text.Trim();
        if (string.Equals(trimmed, "/start", StringComparison.OrdinalIgnoreCase))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Start);
        }

        if (string.Equals(trimmed, "/help", StringComparison.OrdinalIgnoreCase))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Help);
        }

        if (string.Equals(trimmed, "/history", StringComparison.OrdinalIgnoreCase))
        {
            return Command(EquipmentDiagnosticTelegramCommand.History);
        }

        if (string.Equals(trimmed, "/last", StringComparison.OrdinalIgnoreCase))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Last);
        }

        if (string.Equals(trimmed, "/request", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, TelegramDiagnosticConversationService.ServiceRequestButton, StringComparison.Ordinal))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Request);
        }

        if (string.Equals(trimmed, "/requests", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, TelegramDiagnosticConversationService.RequestsButton, StringComparison.Ordinal))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Requests);
        }

        if (string.Equals(trimmed, "/id", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "/whoami", StringComparison.OrdinalIgnoreCase))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Identity);
        }

        if (trimmed.StartsWith("/diagnose", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["/diagnose".Length..].Trim();
            if (trimmed.Length == 0)
            {
                return Invalid("Manufacturer and displayed code are required after /diagnose.");
            }
        }
        else if (trimmed.StartsWith('/'))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Unsupported);
        }

        var lowered = trimmed.ToLowerInvariant();
        var equipmentSide = FindEquipmentSide(lowered);
        var displayContext = FindDisplayContext(lowered);
        var tokens = TokenPattern().Matches(trimmed).Select(match => match.Value).ToArray();
        var code = tokens
            .LastOrDefault(token => LooksLikeCode(token) && !IsHint(token));

        if (code is null)
        {
            return Invalid("A displayed diagnostic code is required.");
        }

        if (ControllerModelNames.Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Unsupported);
        }

        var codeIndex = Array.FindIndex(tokens, token => string.Equals(token, code, StringComparison.OrdinalIgnoreCase));
        var explicitManufacturer = tokens
            .Take(codeIndex)
            .FirstOrDefault(token => !IsHint(token) && !token.StartsWith('/'));
        var manufacturer = explicitManufacturer;

        if (string.IsNullOrWhiteSpace(manufacturer) && !options.RequireExplicitManufacturer)
        {
            manufacturer = options.DefaultManufacturer;
        }

        if (string.IsNullOrWhiteSpace(manufacturer))
        {
            return Invalid("Manufacturer is required. Example: Gree H5.");
        }

        var request = new EquipmentDiagnosticBotRequest(
            Manufacturer: manufacturer,
            Code: code,
            FreeText: options.EnableFreeTextParsing ? trimmed : null,
            Series: DiagnosticRoutingHintExtractor.ExtractSeries(trimmed),
            EquipmentSide: equipmentSide,
            DisplayContext: displayContext,
            PreferredLanguage: options.PreferredLanguage);

        return new EquipmentDiagnosticTelegramParseResult(
            EquipmentDiagnosticTelegramCommand.Diagnose,
            request,
            []);
    }

    public bool TryExtractDiagnosticCode(
        string? text,
        out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith('/'))
        {
            return false;
        }

        var tokens = TokenPattern().Matches(trimmed).Select(match => match.Value).ToArray();
        var match = tokens.LastOrDefault(token => LooksLikeCode(token) && !IsHint(token));
        if (match is null ||
            ControllerModelNames.Contains(match, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        code = match;
        return true;
    }

    private static EquipmentDiagnosticBotEquipmentSide? FindEquipmentSide(string text)
    {
        if (ContainsHint(text, OutdoorHints)) return EquipmentDiagnosticBotEquipmentSide.Outdoor;
        if (ContainsHint(text, IndoorHints)) return EquipmentDiagnosticBotEquipmentSide.Indoor;
        if (ContainsHint(text, ChillerHints)) return EquipmentDiagnosticBotEquipmentSide.Chiller;
        if (ContainsHint(text, ControllerHints)) return EquipmentDiagnosticBotEquipmentSide.Controller;
        return null;
    }

    private static EquipmentDiagnosticBotDisplayContext? FindDisplayContext(string text)
    {
        if (ContainsHint(text, WiredControllerHints)) return EquipmentDiagnosticBotDisplayContext.WiredController;
        if (ContainsHint(text, GatewayHints)) return EquipmentDiagnosticBotDisplayContext.MobileAppOrGateway;
        if (ContainsHint(text, LedHints)) return EquipmentDiagnosticBotDisplayContext.OduMainBoardLed;
        return null;
    }

    private static bool ContainsHint(string text, IEnumerable<string> hints) =>
        hints.Any(hint => text.Contains(hint, StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeCode(string token) =>
        token.Length <= EquipmentDiagnosticBotRequestLimits.Code &&
        (token.Length == 2 && token.All(char.IsDigit) ||
         CodePattern().IsMatch(token) &&
         (token.Any(char.IsDigit) ||
          token.Length == 2 && token.All(IsAsciiLetter) ||
          string.Equals(token, "db", StringComparison.OrdinalIgnoreCase)));

    private static bool IsAsciiLetter(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static bool IsHint(string token) =>
        OutdoorHints.Concat(IndoorHints).Concat(ChillerHints).Concat(ControllerHints)
            .Concat(KnowledgeHints)
            .Concat(["led", "board", "app", "gateway", "wired"])
            .Contains(token, StringComparer.OrdinalIgnoreCase) ||
        DiagnosticRoutingHintExtractor.IsSeriesHintToken(token);

    private static EquipmentDiagnosticTelegramParseResult Invalid(string error) =>
        new(EquipmentDiagnosticTelegramCommand.Diagnose, null, [error]);

    private static EquipmentDiagnosticTelegramParseResult Command(EquipmentDiagnosticTelegramCommand command) =>
        new(command, null, []);

    [GeneratedRegex(@"[\p{L}\p{N}/_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^[\p{L}][\p{L}\p{N}-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
