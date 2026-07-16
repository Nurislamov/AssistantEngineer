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

    private sealed record TokenCandidate(string Value, string NormalizedValue, int StartIndex);

    private sealed record DiagnosticCodeCandidate(string Code, int StartIndex);

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
            string.Equals(trimmed, TelegramDiagnosticConversationService.ServiceRequestButton, StringComparison.Ordinal) ||
            string.Equals(trimmed, TelegramDiagnosticConversationService.PreviousServiceRequestButton, StringComparison.Ordinal))
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
        var tokens = ExtractTokens(trimmed);
        if (!TryExtractNormalizedDiagnosticCode(trimmed, tokens, out var codeCandidate))
        {
            return Invalid("A displayed diagnostic code is required.");
        }

        var code = codeCandidate.Code;
        if (ControllerModelNames.Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            return Command(EquipmentDiagnosticTelegramCommand.Unsupported);
        }

        var explicitManufacturer = tokens
            .Where(token => token.StartIndex < codeCandidate.StartIndex)
            .FirstOrDefault(token => !IsHint(token.NormalizedValue) && !token.Value.StartsWith('/'));
        var manufacturer = explicitManufacturer?.Value;

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

        var tokens = ExtractTokens(trimmed);
        if (!TryExtractNormalizedDiagnosticCode(trimmed, tokens, out var match) ||
            ControllerModelNames.Contains(match.Code, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        code = match.Code;
        return true;
    }

    private static bool TryExtractNormalizedDiagnosticCode(
        string text,
        IReadOnlyList<TokenCandidate> tokens,
        out DiagnosticCodeCandidate code)
    {
        code = ExtractDiagnosticCodeCandidates(text, tokens)
            .OrderBy(candidate => candidate.StartIndex)
            .LastOrDefault()!;
        return code is not null;
    }

    private static IReadOnlyList<DiagnosticCodeCandidate> ExtractDiagnosticCodeCandidates(
        string text,
        IReadOnlyList<TokenCandidate> tokens)
    {
        var candidates = new List<DiagnosticCodeCandidate>();
        foreach (var token in tokens)
        {
            if (!TryNormalizeDiagnosticCodeToken(token, out var code) ||
                IsHint(token.NormalizedValue) ||
                !IsNumericCandidateAllowed(code, text, tokens, token.StartIndex) ||
                !IsLetterOnlyCandidateAllowed(code, text, tokens, token.StartIndex))
            {
                continue;
            }

            candidates.Add(new DiagnosticCodeCandidate(code, token.StartIndex));
        }

        foreach (Match match in SeparatedCodePattern().Matches(text))
        {
            var normalized = EquipmentDiagnosticCodeInputNormalizer.Normalize(match.Value);
            if (TryCompactSeparatedCode(normalized, out var code) &&
                !IsHint(code) &&
                IsSeparatedCodeCandidateAllowed(text, tokens, match))
            {
                candidates.Add(new DiagnosticCodeCandidate(code, match.Index));
            }
        }

        return candidates;
    }

    private static IReadOnlyList<TokenCandidate> ExtractTokens(string text) =>
        TokenPattern()
            .Matches(text)
            .Select(match => new TokenCandidate(
                match.Value,
                EquipmentDiagnosticCodeInputNormalizer.Normalize(match.Value),
                match.Index))
            .ToArray();

    private static bool TryNormalizeDiagnosticCodeToken(
        TokenCandidate token,
        out string code)
    {
        code = string.Empty;
        if (TryCompactSeparatedCode(token.NormalizedValue, out var compact))
        {
            code = compact;
            return true;
        }

        if (!LooksLikeCode(token.NormalizedValue) ||
            IsNonAsciiLetterOnlyCode(token))
        {
            return false;
        }

        code = token.NormalizedValue;
        return true;
    }

    private static bool IsNonAsciiLetterOnlyCode(TokenCandidate token) =>
        token.NormalizedValue.Length == 2 &&
        token.NormalizedValue.All(IsAsciiLetter) &&
        !token.Value.All(IsAsciiLetter);

    private static bool TryCompactSeparatedCode(
        string token,
        out string code)
    {
        code = string.Empty;
        if (!SeparatedCodeShapePattern().IsMatch(token))
        {
            return false;
        }

        code = new string(token.Where(character => !IsSimpleCodeSeparator(character)).ToArray());
        return code.Length == 2;
    }

    private static bool IsNumericCandidateAllowed(
        string code,
        string text,
        IReadOnlyList<TokenCandidate> tokens,
        int startIndex)
    {
        if (!code.All(char.IsDigit))
        {
            return true;
        }

        var trimmed = text.Trim();
        if (string.Equals(trimmed, code, StringComparison.Ordinal))
        {
            return true;
        }

        return tokens
            .Where(token => token.StartIndex < startIndex)
            .Any(token => IsAsciiWord(token.Value) &&
                (!IsHint(token.NormalizedValue) ||
                 DiagnosticRoutingHintExtractor.IsSeriesHintToken(token.NormalizedValue)));
    }

    private static bool IsLetterOnlyCandidateAllowed(
        string code,
        string text,
        IReadOnlyList<TokenCandidate> tokens,
        int startIndex)
    {
        if (code.Length != 2 || !code.All(IsAsciiLetter))
        {
            return true;
        }

        if (IsWholeMessageTokenCandidate(text, code))
        {
            return true;
        }

        if (!IsCandidateAtMessageEnd(text, startIndex + code.Length))
        {
            return false;
        }

        if (HasManufacturerOrSeriesContextBeforeCandidate(text, tokens, startIndex) ||
            HasStrongDiagnosticContextBeforeCandidate(text, startIndex))
        {
            return true;
        }

        return HasWeakDiagnosticContextBeforeCandidate(text, startIndex);
    }

    private static bool IsSeparatedCodeCandidateAllowed(
        string text,
        IReadOnlyList<TokenCandidate> tokens,
        Match match)
    {
        if (match.Value.Any(character => character is '-' or '.' or '_'))
        {
            return true;
        }

        if (IsWholeMessageSeparatedCandidate(text, match.Value))
        {
            return true;
        }

        if (!IsCandidateAtMessageEnd(text, match.Index + match.Length))
        {
            return false;
        }

        if (HasManufacturerOrSeriesContextBeforeCandidate(text, tokens, match.Index))
        {
            return true;
        }

        if (HasStrongDiagnosticContextBeforeCandidate(text, match.Index))
        {
            return true;
        }

        return HasWeakDiagnosticContextBeforeCandidate(text, match.Index);
    }

    private static bool IsWholeMessageSeparatedCandidate(
        string text,
        string candidate)
    {
        var trimmed = text.Trim()
            .Trim('(', ')', '[', ']', '{', '}', '.', ',', ':', ';', '!', '?', '"', '\'');
        return string.Equals(trimmed, candidate, StringComparison.Ordinal);
    }

    private static bool IsWholeMessageTokenCandidate(
        string text,
        string candidate)
    {
        var trimmed = text.Trim()
            .Trim('(', ')', '[', ']', '{', '}', '.', ',', ':', ';', '!', '?', '"', '\'');
        return string.Equals(trimmed, candidate, StringComparison.Ordinal);
    }

    private static bool HasManufacturerOrSeriesContextBeforeCandidate(
        string text,
        IReadOnlyList<TokenCandidate> tokens,
        int candidateStartIndex)
    {
        if (DiagnosticRoutingHintExtractor.ExtractSeries(text[..candidateStartIndex]) is not null)
        {
            return true;
        }

        return tokens
            .Where(token => token.StartIndex < candidateStartIndex)
            .Any(token => string.Equals(token.Value, "Gree", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasStrongDiagnosticContextBeforeCandidate(
        string text,
        int candidateStartIndex) =>
        StrongDiagnosticContextBeforeCandidatePattern().IsMatch(text[..candidateStartIndex]);

    private static bool HasWeakDiagnosticContextBeforeCandidate(
        string text,
        int candidateStartIndex) =>
        WeakDiagnosticContextBeforeCandidatePattern().IsMatch(text[..candidateStartIndex]);

    private static bool IsCandidateAtMessageEnd(
        string text,
        int candidateEndIndex) =>
        text[candidateEndIndex..].All(character =>
            char.IsWhiteSpace(character) ||
            character is '.' or ',' or ':' or ';' or '!' or '?' or ')' or ']' or '}');

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

    private static bool IsAsciiWord(string value) =>
        value.Length > 0 && value.All(IsAsciiLetter);

    private static bool IsSimpleCodeSeparator(char value) =>
        value is '-' or '_' or '.' || char.IsWhiteSpace(value);

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

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[\p{L}]\s*[-._\s]\s*[\p{L}\p{N}](?![\p{L}\p{N}])", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatedCodePattern();

    [GeneratedRegex(@"^[A-Za-z]\s*[-._\s]\s*[A-Za-z0-9]$", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatedCodeShapePattern();

    [GeneratedRegex(@"(?:^|[\s([{])(?:код|code|/diagnose)[\s:;,\-]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StrongDiagnosticContextBeforeCandidatePattern();

    [GeneratedRegex(@"(?:^|[\s([{])(?:(?:ошибка|error|fault|показывает)|(?:на\s+(?:пульте|контроллере))|(?:контроллер\s+показывает))[\s:;,\-]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WeakDiagnosticContextBeforeCandidatePattern();
}
