using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXFinalClosureTests
{
    private static readonly string GreeRoot = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");

    private static readonly string[] ForbiddenVisibleFragments =
    [
        "Подтвердите код",
        "Сверьте модель",
        "Дальнейшие действия",
        "Точная причина зависит",
        "source",
        "manual",
        "packageId",
        "руководство",
        "основание",
        "по таблице",
        "классифицирован по таблице",
        "карточка неисправности"
    ];

    [Fact]
    public void GmvXRuntimeCountsAreStableAtFinalClosure()
    {
        Assert.Equal(1296, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(GmvXRoot, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(GmvXRoot, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(GmvXRoot, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(GmvXRoot, "debugging"), "*.json").Length);
    }

    [Fact]
    public void AllGmvXTitlesUseExactSeriesName()
    {
        foreach (var entry in ReadGmvXEntries())
        {
            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
                var title = RequiredString(text, "title");
                Assert.Contains("Gree GMV X", title, StringComparison.Ordinal);
                Assert.DoesNotContain("Gree GMV6", title, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Gree GMV —", title, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void AllGmvXVisibleTextIsFreeOfGenericTemplateAndSourceLeakage()
    {
        foreach (var entry in ReadGmvXEntries())
        {
            var visible = VisibleBlob(entry);
            foreach (var forbidden in ForbiddenVisibleFragments)
            {
                Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Gmv6RemainsClosedAndUntouchedByGmvXFinalClosure()
    {
        var gmv6Root = Path.Combine(GreeRoot, "gmv6");

        Assert.Equal(263, Directory.GetFiles(gmv6Root, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(gmv6Root, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(gmv6Root, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(gmv6Root, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(gmv6Root, "debugging"), "*.json").Length);

        foreach (var entry in Directory.GetFiles(gmv6Root, "*.json", SearchOption.AllDirectories).Select(ReadObject))
        {
            Assert.Equal("GMV6", RequiredString(entry, "series"));
            Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
        }
    }

    private static JsonObject[] ReadGmvXEntries() =>
        Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(ReadObject)
            .ToArray();

    private static string VisibleBlob(JsonObject entry)
    {
        var parts = new List<string>();
        foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
        {
            foreach (var propertyName in new[] { "title", "summary", "recommendedAction", "safetyNote", "sourceNote" })
            {
                parts.Add(RequiredString(text, propertyName));
            }

            foreach (var propertyName in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
            {
                parts.AddRange(RequiredArray(text, propertyName).OfType<JsonValue>().Select(value => value.GetValue<string>()));
            }
        }

        return string.Join('\n', parts);
    }

    private static JsonObject ReadObject(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        return Assert.IsType<JsonObject>(node);
    }

    private static string RequiredString(JsonObject entry, string propertyName)
    {
        Assert.True(entry.TryGetPropertyValue(propertyName, out var node), $"Missing property '{propertyName}'.");
        return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
    }

    private static JsonArray RequiredArray(JsonObject entry, string propertyName)
    {
        Assert.True(entry.TryGetPropertyValue(propertyName, out var node), $"Missing property '{propertyName}'.");
        return Assert.IsType<JsonArray>(node);
    }
}
