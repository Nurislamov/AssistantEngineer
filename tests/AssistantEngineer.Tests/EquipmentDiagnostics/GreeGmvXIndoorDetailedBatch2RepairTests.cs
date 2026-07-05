using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXIndoorDetailedBatch2RepairTests
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
        "L0", "L1", "L3", "L4", "L5", "L7", "L9", "LA", "LC", "LF", "LU",
        "o3", "o7", "o8", "o9", "y7", "y8", "yA"
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
        "к датчика"
    ];

    [Fact]
    public void AllStageCodesExistAndHaveCleanGmvXTitles()
    {
        var entries = ReadStageEntries();

        Assert.Equal(18, entries.Count);
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
    public void IndoorMeaningsAndProceduresStaySpecific()
    {
        AssertText("L0", ["общей индикацией", "C01", "инженерного номера"]);
        AssertText("L1", ["вращается слишком медленно", "DC, AC или PG", "защиты двигателя от перегрузки"]);
        AssertText("L3", ["поплавкового выключателя", "дренажную трубу", "дренажного насоса"]);
        AssertText("L4", ["чрезмерный ток", "проводного пульта", "короткое замыкание"]);
        AssertText("L5", ["замерзание испарителя", "фильтр и испаритель", "недостаток хладагента"]);
        AssertText("L7", ["главный внутренний блок", "одной системе электропитания", "заново назначьте"]);
        AssertText("L9", ["более 16", "P14", "фактическое число"]);
        AssertText("LA", ["разных серий", "одной серии"]);
        AssertText("LC", ["только к некоторым", "не распознаёт", "совместимость"]);
        AssertText("LF", ["гидромодулю", "распределительного клапана", "конфликт инженерного номера"]);
        AssertText("LU", ["системы рекуперации тепла", "одной ветви", "распределителя режимов"]);
        AssertText("o3", ["IPM", "внешним приводом DC-вентилятора", "U, V и W", "2 МОм"]);
        AssertText("o7", ["рассинхронизацию", "внешним приводом DC-вентилятора", "U, V и W"]);
        AssertText("o8", ["не получает данные", "30 секунд", "платой привода"]);
        AssertText("o9", ["DC-двигателем", "30 секунд", "основной платой"]);
        AssertText("y7", ["приточного воздуха", "AD-значение", "5 секунд"]);
        AssertText("y8", ["воздушным модулем", "CO2", "PM2.5"]);
        AssertText("yA", ["IFD-модуля", "60 секунд", "5 секунд"]);
    }

    [Fact]
    public void StageRuntimeCountsRemainStableAndGmv6IsUntouched()
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
        return Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));
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
