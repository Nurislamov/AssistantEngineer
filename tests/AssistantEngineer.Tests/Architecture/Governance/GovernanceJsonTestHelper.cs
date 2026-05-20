using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture.Governance;

internal static class GovernanceJsonTestHelper
{
    public static JsonDocument Parse(string path)
    {
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    public static void AssertJsonAndSchemaParseIfPresent(string jsonPath)
    {
        _ = JsonDocument.Parse(File.ReadAllText(jsonPath));

        var schemaPath = Path.Combine(
            Path.GetDirectoryName(jsonPath)!,
            $"{Path.GetFileNameWithoutExtension(jsonPath)}.schema.json");

        if (File.Exists(schemaPath))
            _ = JsonDocument.Parse(File.ReadAllText(schemaPath));
    }

    public static HashSet<string> StringSet(JsonElement arrayElement)
    {
        return arrayElement
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static void AssertBooleanPropertiesFalse(JsonElement element, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
            Assert.False(element.GetProperty(propertyName).GetBoolean());
    }

    public static void AssertArrayContainsAny(JsonElement arrayElement, string containsText)
    {
        var values = arrayElement.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(values, value => value.Contains(containsText, StringComparison.OrdinalIgnoreCase));
    }
}
