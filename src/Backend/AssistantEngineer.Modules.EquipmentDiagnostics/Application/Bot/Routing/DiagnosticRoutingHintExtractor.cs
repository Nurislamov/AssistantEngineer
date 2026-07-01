using System.Text.RegularExpressions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot.Routing;

public static partial class DiagnosticRoutingHintExtractor
{
    public static string? ExtractSeries(string? text)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Contains("UMATCH", StringComparison.Ordinal) ||
            normalized.Contains("GUD", StringComparison.Ordinal) ||
            normalized.Contains("ПОЛУПРОМ", StringComparison.Ordinal) ||
            normalized.Contains("ПОЛУПРОМЫШЛЕН", StringComparison.Ordinal) ||
            HasTokenPair(text, "U", "MATCH") ||
            HasToken(text, "UMATCH") ||
            HasToken(text, "U-MATCH"))
        {
            return "U-Match R32";
        }

        if (normalized.Contains("ERV", StringComparison.Ordinal) ||
            normalized.Contains("FHBQG", StringComparison.Ordinal) ||
            normalized.Contains("ВЕНТИЛЯЦ", StringComparison.Ordinal) ||
            normalized.Contains("РЕКУПЕРАТ", StringComparison.Ordinal) ||
            normalized.Contains("ENERGYRECOVERYVENTILATION", StringComparison.Ordinal) ||
            HasTokenPair(text, "ENERGY", "RECOVERY") ||
            HasTokenPair(text, "RECOVERY", "VENTILATION"))
        {
            return "ERV B Series";
        }

        if (normalized.Contains("GMV6HR", StringComparison.Ordinal) ||
            normalized.Contains("GMVHR", StringComparison.Ordinal) ||
            normalized.Contains("HEATRECOVERY", StringComparison.Ordinal) ||
            HasTokenPair(text, "GMV6", "HR") ||
            HasTokenPair(text, "GMV", "HR") ||
            HasTokenPair(text, "GMV6", "HEAT") ||
            HasTokenPair(text, "GMV6", "RECOVERY"))
        {
            return "GMV6 HR";
        }

        if (normalized.Contains("GMV9FLEX", StringComparison.Ordinal) ||
            normalized.Contains("9SERIESFLEX", StringComparison.Ordinal) ||
            normalized.Contains("9FLEX", StringComparison.Ordinal) ||
            normalized.Contains("GMV9", StringComparison.Ordinal) ||
            HasTokenPair(text, "GMV9", "FLEX") ||
            HasTokenPair(text, "9", "FLEX"))
        {
            return "GMV9 Flex";
        }

        if (normalized.Contains("GMVXPRO", StringComparison.Ordinal))
        {
            return "GMV X PRO";
        }

        if (normalized.Contains("GMVX", StringComparison.Ordinal) ||
            normalized.Contains("XSERIES", StringComparison.Ordinal) ||
            HasToken(text, "X"))
        {
            return "GMV X";
        }

        if (normalized.Contains("GMV5MINI", StringComparison.Ordinal) ||
            normalized.Contains("GMVMINI", StringComparison.Ordinal) ||
            HasTokenPair(text, "GMV", "MINI") ||
            HasToken(text, "MINI"))
        {
            return "GMV Mini";
        }

        if (normalized.Contains("GMV5SLIM", StringComparison.Ordinal) ||
            normalized.Contains("GMVSLIM", StringComparison.Ordinal) ||
            HasTokenPair(text, "GMV", "SLIM") ||
            HasToken(text, "SLIM"))
        {
            return "GMV Slim";
        }

        if (normalized.Contains("GMV6", StringComparison.Ordinal))
        {
            return "GMV6";
        }

        return null;
    }

    public static bool IsSeriesHintToken(string token) =>
        token.Equals("GMV", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("U", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("MATCH", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("U-MATCH", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("UMATCH", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GUD", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("ПОЛУПРОМ", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("ПОЛУПРОМЫШЛЕННЫЙ", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("ERV", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("FHBQG", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("ВЕНТИЛЯЦИЯ", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("РЕКУПЕРАТОР", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("ENERGY", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("VENTILATION", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV5", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV6", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("HR", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("HEAT", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("RECOVERY", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV9", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("9", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("SERIES", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("MINI", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("FLEX", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("SLIM", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("PRO", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("X", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("9-SERIES", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("9-FLEX", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("9-SERIES-FLEX", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV9-FLEX", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV6-HR", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("HEAT-RECOVERY", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("X-SERIES", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV-MINI", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV5-MINI", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV-SLIM", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("GMV5-SLIM", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesSeries(string? candidateSeries, string? requestedSeries)
    {
        if (string.IsNullOrWhiteSpace(requestedSeries) ||
            string.IsNullOrWhiteSpace(candidateSeries))
        {
            return true;
        }

        return Normalize(candidateSeries) == Normalize(requestedSeries) ||
               (string.Equals(requestedSeries, "U-Match", StringComparison.OrdinalIgnoreCase) &&
                candidateSeries.StartsWith("U-Match", StringComparison.OrdinalIgnoreCase)) ||
               (string.Equals(requestedSeries, "UMatch", StringComparison.OrdinalIgnoreCase) &&
                candidateSeries.StartsWith("U-Match", StringComparison.OrdinalIgnoreCase)) ||
               (string.Equals(requestedSeries, "ERV", StringComparison.OrdinalIgnoreCase) &&
                candidateSeries.StartsWith("ERV", StringComparison.OrdinalIgnoreCase)) ||
               (string.Equals(requestedSeries, "GMV", StringComparison.OrdinalIgnoreCase) &&
                candidateSeries.StartsWith("GMV", StringComparison.OrdinalIgnoreCase));
    }

    public static string ContextLabel(string manufacturer, string? series) =>
        string.IsNullOrWhiteSpace(series)
            ? manufacturer
            : $"{manufacturer} {series}";

    private static bool HasToken(string? text, string token) =>
        Tokens(text).Any(value => value.Equals(token, StringComparison.OrdinalIgnoreCase));

    private static bool HasTokenPair(string? text, string first, string second)
    {
        var tokens = Tokens(text);
        return tokens.Contains(first, StringComparer.OrdinalIgnoreCase) &&
               tokens.Contains(second, StringComparer.OrdinalIgnoreCase);
    }

    private static string[] Tokens(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : TokenPattern().Matches(text).Select(match => match.Value).ToArray();

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    [GeneratedRegex(@"[\p{L}\p{N}/_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
