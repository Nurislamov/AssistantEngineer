using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXOutdoorEFRepairTests
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
        "E1", "E2", "E3", "E4", "Ed", "F0", "F1", "F3", "F5", "F6", "F7", "F8"
    ];

    private static readonly string[] ForbiddenVisibleFragments =
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
        "классифицирован по таблице",
        "замените датчик",
        "замените плату",
        "замените компрессор",
        "принудительный запуск"
    ];

    [Fact]
    public void AllStage2OutdoorEfCodesExistAndAreRepaired()
    {
        var entries = ReadStageEntries();

        Assert.Equal(12, entries.Count);
        Assert.Equal(StageCodes.Order(StringComparer.Ordinal), entries.Keys.Order(StringComparer.Ordinal));

        foreach (var (code, entry) in entries)
        {
            var visible = VisibleBlob(entry);
            Assert.Contains($"Gree GMV X — {code} —", visible, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV6", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree GMV —", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var forbidden in ForbiddenVisibleFragments)
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
    public void ProtectionAndPressureSensorMeaningsStaySpecific()
    {
        AssertText("E1", ["высок", "давлен", "65", "4.2 MPa"]);
        AssertText("E2", ["низкой температуре нагнетания", "10"]);
        AssertText("E3", ["низкому давлению", "-41"]);
        AssertText("E4", ["высокой температуре нагнетания", "118"]);
        AssertText("Ed", ["низкой температуре", "IPM", "плата привода"]);
        AssertText("F0", ["основной платы наружного блока", "адрес", "памяти", "clock"]);
        AssertText("F1", ["датчика высокого давления", "AD", "30 секунд"]);
        AssertText("F3", ["датчика низкого давления", "AD", "30 секунд"]);
    }

    [Fact]
    public void CompressorDischargeSensorNumbersStaySpecific()
    {
        AssertText("F5", ["температуры нагнетания компрессора 1", "AD", "30 секунд"]);
        AssertText("F6", ["температуры нагнетания компрессора 2", "AD", "30 секунд"]);
        AssertText("F7", ["температуры нагнетания компрессора 3", "AD", "30 секунд"]);
        AssertText("F8", ["температуры нагнетания компрессора 4", "AD", "30 секунд"]);
    }

    [Fact]
    public void Stage2InventoryCountsMatchExpected()
    {
        var entries = Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories)
            .Select(ReadObject)
            .Select(entry => RequiredString(entry, "code"))
            .ToArray();

        var alreadyRepaired = 33 + 13 + StageCodes.Length;
        var detailedRemaining = 132 - 13 - StageCodes.Length;

        Assert.Equal(58, alreadyRepaired);
        Assert.Equal(107, detailedRemaining);
        Assert.Equal(263, entries.Length);
        Assert.Equal(1296, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
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
