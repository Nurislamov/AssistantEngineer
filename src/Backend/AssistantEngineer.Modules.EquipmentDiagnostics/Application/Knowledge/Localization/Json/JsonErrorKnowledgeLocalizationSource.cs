using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

public sealed class JsonErrorKnowledgeLocalizationSource : IErrorKnowledgeLocalizationSource
{
    private readonly Lazy<IReadOnlyCollection<ErrorKnowledgeEntryV2>> _entries;

    public JsonErrorKnowledgeLocalizationSource()
        : this(new ErrorKnowledgeJsonLoader())
    {
    }

    public JsonErrorKnowledgeLocalizationSource(ErrorKnowledgeJsonLoader loader)
    {
        _entries = new(
            () => loader.LoadFromAssembly(typeof(JsonErrorKnowledgeLocalizationSource).Assembly),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => _entries.Value;

    public ErrorKnowledgeLocalizationSelection? Select(
        EquipmentDiagnosticBotResponse response,
        string locale,
        ErrorKnowledgeAudience audience)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var entry = GetEntries().FirstOrDefault(item =>
            item.Manufacturer.Equals(response.NormalizedManufacturer, StringComparison.OrdinalIgnoreCase) &&
            item.Code.Equals(response.NormalizedCode, StringComparison.OrdinalIgnoreCase) &&
            MatchesSeries(item.Series, response.EquipmentContext?.Series) &&
            Matches(item.Models, response.EquipmentContext?.ModelCode));
        var text = entry?.Texts.FirstOrDefault(item =>
            item.Locale.Equals(normalizedLocale, StringComparison.OrdinalIgnoreCase) &&
            item.Audience == audience);
        return entry is null || text is null
            ? null
            : new ErrorKnowledgeLocalizationSelection(entry, text);
    }

    public static IReadOnlyCollection<string> GetEmbeddedResourceNames() =>
        typeof(JsonErrorKnowledgeLocalizationSource).Assembly
            .GetManifestResourceNames()
            .Where(ErrorKnowledgeJsonLoader.IsKnowledgeResource)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static bool Matches(string? expected, string? actual) =>
        string.IsNullOrWhiteSpace(expected) ||
        string.IsNullOrWhiteSpace(actual) ||
        expected.Equals(actual, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesSeries(string? expected, string? actual) =>
        Matches(expected, actual) ||
        string.Equals(actual, "GMV", StringComparison.OrdinalIgnoreCase) &&
        expected?.StartsWith("GMV", StringComparison.OrdinalIgnoreCase) == true;

    private static bool Matches(IReadOnlyCollection<string> expected, string? actual) =>
        expected.Count == 0 ||
        string.IsNullOrWhiteSpace(actual) ||
        expected.Contains(actual, StringComparer.OrdinalIgnoreCase);

    private static string NormalizeLocale(string locale)
    {
        var normalized = locale.Trim().Replace('_', '-').ToLowerInvariant();
        var separator = normalized.IndexOf('-');
        return separator < 0 ? normalized : normalized[..separator];
    }
}
