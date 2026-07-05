using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXIndoorDetailedBatch1RepairTests
{
    private static readonly string GreeRoot = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string IndoorRoot = Path.Combine(GmvXRoot, "indoor");

    private static readonly string[] StageCodes =
    [
        "d1", "d3", "d4", "d6", "d7", "d9", "dA", "dC", "dd", "dF", "dH", "dL", "dn", "dP"
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
        "принудительный запуск",
        "hydro box",
        "jump silk screen"
    ];

    [Fact]
    public void AllStageIndoorCodesExistAndAreRepaired()
    {
        var entries = ReadStageEntries();

        Assert.Equal(14, entries.Count);
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
    public void IndoorMeaningsStaySpecificToManualProcedures()
    {
        AssertText("d1", ["адресный чип", "чип памяти", "основную плату внутреннего блока"]);
        AssertText("d3", ["датчика температуры воздуха", "AD-значение", "5 секунд", "разъём"]);
        AssertText("d4", ["датчика температуры входной трубки", "AD-значение", "5 секунд"]);
        AssertText("d6", ["датчика температуры выходной трубки", "AD-значение", "5 секунд"]);
        AssertText("d7", ["датчика влажности", "AD-значение", "5 секунд"]);
        AssertText("d9", ["перемыч", "номер перемычки", "цепь детекции"]);
        AssertText("dA", ["IP-адрес", "равен 0", "конфликт IP-адресов", "адресный чип"]);
        AssertText("dC", ["DIP-переключателя мощности", "цепь детекции"]);
        AssertText("dH", ["IIC-связь", "сенсорной клавиатурой", "дисплейной платой"]);
        AssertText("dL", ["датчика температуры воздуха на выходе", "AD-значение", "5 секунд"]);
        AssertText("dF", ["датчика температуры гидромодуля", "AD-значение", "30 секунд"]);
        AssertText("dP", ["входа воды теплого пола", "AD-значение", "30 секунд"]);
        AssertText("dd", ["солнечной системы", "AD-значение", "5 секунд"]);
        AssertText("dn", ["механизма качания", "концевым выключателем", "CAN-данные"]);
    }

    [Fact]
    public void StageIndoorRuntimeCountsRemainStable()
    {
        Assert.Equal(1308, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(GmvXRoot, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(IndoorRoot, "*.json").Length);
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
        return Directory.GetFiles(IndoorRoot, "*.json")
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
