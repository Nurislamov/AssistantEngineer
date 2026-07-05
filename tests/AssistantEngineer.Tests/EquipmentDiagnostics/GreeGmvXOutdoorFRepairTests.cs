using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXOutdoorFRepairTests
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
        "F9", "FA", "Fb", "FC", "Fd", "FE", "FF", "FH", "FJ", "FL", "Fn", "FU"
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
    public void AllStage3OutdoorFCodesExistAndAreRepaired()
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
    public void TemperatureSensorMeaningsStaySpecific()
    {
        AssertText("F9", ["температуры нагнетания компрессора 5", "AD", "30 секунд"]);
        AssertText("FA", ["температуры нагнетания компрессора 6", "AD", "30 секунд"]);
        AssertText("FU", ["верхней части корпуса компрессора 1", "AD", "30 секунд"]);
        AssertText("Fb", ["верхней части корпуса компрессора 2", "AD", "30 секунд"]);
        AssertText("Fd", ["выходной трубы блока переключения режимов", "AD", "5 секунд"]);
        AssertText("Fn", ["входной трубы блока переключения режимов", "AD", "5 секунд"]);
    }

    [Fact]
    public void CurrentSensorMeaningsStaySpecificIncludingFh()
    {
        AssertText("FH", ["датчик тока компрессора 1", "AD", "3 секунд"]);
        AssertText("FC", ["датчик тока компрессора 2", "AD", "3 секунд"]);
        AssertText("FL", ["датчик тока компрессора 3", "AD", "3 секунд"]);
        AssertText("FE", ["датчик тока компрессора 4", "AD", "3 секунд"]);
        AssertText("FF", ["датчик тока компрессора 5", "AD", "3 секунд"]);
        AssertText("FJ", ["датчик тока компрессора 6", "AD", "3 секунд"]);
    }

    [Fact]
    public void Stage3RuntimeCountsRemainStable()
    {
        Assert.Equal(1308, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
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
