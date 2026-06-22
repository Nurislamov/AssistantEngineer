using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;

public sealed class TelegramManualRegistrySource : ITelegramManualRegistrySource
{
    private const string DefaultRegistryPath = "data/equipment-diagnostics/manual-library/manuals.json";
    private const string EmbeddedRegistryResourceSuffix = ".Knowledge.ManualLibrary.manuals.json";
    private readonly Lazy<IReadOnlyList<TelegramManualRegistryEntry>> _manuals = new(LoadManuals);

    public IReadOnlyList<TelegramManualRegistryEntry> GetManuals() => _manuals.Value;

    private static IReadOnlyList<TelegramManualRegistryEntry> LoadManuals()
    {
        var json = File.Exists(DefaultRegistryPath)
            ? File.ReadAllText(DefaultRegistryPath)
            : ReadEmbeddedRegistry();
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("manuals", out var manuals) ||
            manuals.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<TelegramManualRegistryEntry>();
        foreach (var manual in manuals.EnumerateArray())
        {
            var manualId = Text(manual, "manualId");
            if (string.IsNullOrWhiteSpace(manualId))
            {
                continue;
            }

            var policy = manual.TryGetProperty("futureTelegramManualLibrary", out var futurePolicy)
                ? futurePolicy
                : default;
            result.Add(new TelegramManualRegistryEntry(
                manualId,
                Text(manual, "fileName") ?? string.Empty,
                Text(manual, "documentTitle") ?? manualId,
                Text(manual, "documentCode"),
                Text(manual, "fileFormat") ?? string.Empty,
                Bool(policy, "eligibleForTelegramLibrary"),
                Roles(policy, "allowedRoles"),
                Roles(policy, "deniedRoles")));
        }

        return result
            .OrderBy(manual => manual.ManualId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ReadEmbeddedRegistry()
    {
        var assembly = typeof(TelegramManualRegistrySource).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(EmbeddedRegistryResourceSuffix, StringComparison.Ordinal));
        if (resourceName is null)
        {
            return """{"manuals":[]}""";
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return """{"manuals":[]}""";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string? Text(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool Bool(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.True;

    private static IReadOnlySet<TelegramUserRole> Roles(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            return new HashSet<TelegramUserRole>();
        }

        return values
            .EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Enum.TryParse<TelegramUserRole>(value, ignoreCase: true, out var role)
                ? role
                : (TelegramUserRole?)null)
            .Where(role => role is not null)
            .Select(role => role!.Value)
            .ToHashSet();
    }
}
