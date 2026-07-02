using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXOutdoorSensorRepairTests
{
    private static readonly string GreeRoot = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string OutdoorRoot = Path.Combine(GmvXRoot, "outdoor");

    private static readonly string[] StageCodes =
    [
        "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "bA", "bd", "bJ", "bn"
    ];

    private static readonly string[] GenericAndSourceFragments =
    [
        "Подтвердите код",
        "Сверьте модель",
        "Дальнейшие действия",
        "Точная причина зависит",
        "manual",
        "source",
        "packageId",
        "руководство",
        "основание",
        "по таблице",
        "классифицирован по таблице"
    ];

    [Fact]
    public void AllStage1OutdoorSensorCodesExistAndUseGmvXManualWording()
    {
        var entries = ReadStageEntries();

        Assert.Equal(13, entries.Count);
        Assert.Equal(StageCodes.Order(StringComparer.Ordinal), entries.Keys.Order(StringComparer.Ordinal));

        foreach (var (code, entry) in entries)
        {
            var visible = VisibleBlob(entry);
            Assert.Contains($"Gree GMV X — {code} —", visible, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV6", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree GMV —", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var forbidden in GenericAndSourceFragments)
            {
                Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
                Assert.Equal("Служебная заметка не выводится пользователю.", RequiredString(text, "sourceNote"));
            }
        }
    }

    [Fact]
    public void TemperatureSensorBatchKeepsAdThirtySecondDetectionFlow()
    {
        var entries = ReadStageEntries();

        foreach (var code in new[] { "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "bA", "bd", "bn" })
        {
            var visible = VisibleBlob(entries[code]);
            Assert.Contains("AD", visible, StringComparison.Ordinal);
            Assert.Contains("30 секунд", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("контакт", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("датчик", visible, StringComparison.OrdinalIgnoreCase);
            Assert.True(
                visible.Contains("цепь детекции", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("плата", StringComparison.OrdinalIgnoreCase),
                visible);
        }

        AssertText("b1", ["температуры наружного воздуха"]);
        AssertText("b2", ["оттайки 1"]);
        AssertText("b3", ["оттайки 2"]);
        AssertText("b4", ["выхода жидкости", "переохлад"]);
        AssertText("b5", ["выхода газа", "переохлад"]);
        AssertText("b6", ["всасывания 1"]);
        AssertText("b7", ["всасывания 2"]);
        AssertText("b8", ["влажности наружного воздуха"]);
        AssertText("b9", ["выхода газа", "теплообмен"]);
        AssertText("bA", ["возврата масла"]);
        AssertText("bd", ["входа воздуха", "переохлад"]);
        AssertText("bn", ["жидкости", "переохлад"]);
    }

    [Fact]
    public void BjKeepsPressureSensorReverseConnectionFlow()
    {
        var visible = VisibleBlob(ReadStageEntries()["bJ"]);

        Assert.Contains("датчик высокого давления", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("датчик низкого давления", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("4.9-5.1 V", visible, StringComparison.Ordinal);
        Assert.Contains("0.5-4.5 V", visible, StringComparison.Ordinal);
        Assert.Contains("клемм", visible, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("по таблице", visible, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuntimeCountsRemainStableAfterOutdoorSensorRepair()
    {
        Assert.Equal(1296, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(OutdoorRoot, "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(GmvXRoot, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(GmvXRoot, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(GmvXRoot, "debugging"), "*.json").Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRoot, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
    }

    private static void AssertText(string code, string[] contains)
    {
        var visible = VisibleBlob(ReadStageEntries()[code]);
        foreach (var fragment in contains)
        {
            Assert.Contains(fragment, visible, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, JsonObject> ReadStageEntries()
    {
        var codes = StageCodes.ToHashSet(StringComparer.Ordinal);
        return Directory.GetFiles(OutdoorRoot, "*.json")
            .Select(ReadObject)
            .Where(entry => codes.Contains(RequiredString(entry, "code")))
            .ToDictionary(entry => RequiredString(entry, "code"), StringComparer.Ordinal);
    }

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
                parts.AddRange(RequiredArray(text, propertyName)
                    .OfType<JsonValue>()
                    .Select(value => value.GetValue<string>()));
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
