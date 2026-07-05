using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXIndoorRemainingRepairTests
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
        "d2", "dJ", "dU", "dy",
        "L8", "Lb", "LE", "LJ", "LL", "LP",
        "o0", "o1", "o2", "o4", "o5", "o6", "oA", "ob", "oC",
        "y1", "y2"
    ];

    private static readonly string[] DetailedCodes = ["d2", "dJ", "dU", "LL"];

    private static readonly string[] TableOnlyCodes =
    [
        "dy",
        "L8", "Lb", "LE", "LJ", "LP",
        "o0", "o1", "o2", "o4", "o5", "o6", "oA", "ob", "oC",
        "y1", "y2"
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
    public void AllScopedIndoorRemainingCodesExistWithGmvXTitleAndSafeVisibleText()
    {
        var entries = ReadStageEntries();

        Assert.Equal(21, entries.Count);
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
        }
    }

    [Fact]
    public void DetailedIndoorHydroBoxCodesKeepExactTroubleshootingBoundaries()
    {
        AssertText("d2", [
            "бак",
            "датчик температуры бака воды",
            "AD-знач",
            "5 секунд",
            "основную плату гидромодуля"
        ]);

        AssertText("dJ", [
            "обратной воды",
            "датчик температуры обратной воды",
            "AD-значение",
            "5 секунд",
            "основную плату гидромодуля"
        ]);

        AssertText("dU", [
            "тёплого пола",
            "выходной трубы воды",
            "датчик температуры",
            "AD-знач",
            "циркуляционном водяном контуре",
            "основную плату гидромодуля"
        ]);

        AssertText("LL", [
            "реле протока воды",
            "15 секунд",
            "генератора",
            "пополните контур водой",
            "основную плату гидромодуля"
        ]);

        foreach (var code in DetailedCodes)
        {
            var entry = ReadStageEntries()[code];
            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
                Assert.NotEmpty(RequiredArray(text, "possibleCauses"));
                Assert.NotEmpty(RequiredArray(text, "checkSteps"));
            }
        }
    }

    [Fact]
    public void RemainingIndoorTableOnlyCodesStayShortAndDoNotInventCauses()
    {
        var entries = ReadStageEntries();

        foreach (var code in TableOnlyCodes)
        {
            var entry = entries[code];
            var visible = VisibleBlob(entry);

            Assert.DoesNotContain("пошаговая диагностика", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("подробная процедура", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("расширенная процедура", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("замените плату", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("замените датчик", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("замените мотор", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("замените контроллер", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
                Assert.Empty(RequiredArray(text, "possibleCauses"));

                var steps = string.Join(' ', RequiredArray(text, "checkSteps")
                    .OfType<JsonValue>()
                    .Select(value => value.GetValue<string>()));

                Assert.Contains($"Зафиксируйте код {code}", steps, StringComparison.Ordinal);
                Assert.Contains("внутреннего блока Gree GMV X", steps, StringComparison.Ordinal);
                Assert.Contains("сервисному инженеру", steps, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void RepresentativeTableOnlyMeaningsRemainSpecific()
    {
        AssertText("dy", ["датчика температуры воды"]);
        AssertText("L8", ["недостаточная мощность питания"]);
        AssertText("Lb", ["повторного нагрева и осушения"]);
        AssertText("LE", ["EC DC водяного насоса"]);
        AssertText("LJ", ["функционального DIP-переключателя"]);
        AssertText("LP", ["перехода через ноль PG-двигателя"]);
        AssertText("o0", ["другая неисправность привода"]);
        AssertText("o1", ["низкое напряжение DC-шины внутреннего блока"]);
        AssertText("o2", ["высокое напряжение DC-шины внутреннего блока"]);
        AssertText("o4", ["сбой запуска внутреннего блока"]);
        AssertText("o5", ["защита внутреннего блока от сверхтока"]);
        AssertText("o6", ["цепи определения тока внутреннего блока"]);
        AssertText("oA", ["высокая температура модуля внутреннего блока"]);
        AssertText("ob", ["датчика температуры модуля внутреннего блока"]);
        AssertText("oC", ["цепи заряда внутреннего блока"]);
        AssertText("y1", ["датчика температуры входной трубки 2"]);
        AssertText("y2", ["датчика температуры выходной трубки 2"]);
    }

    [Fact]
    public void InventoryAndRuntimeCountsMatchEd24Gmvx15()
    {
        var entries = Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories)
            .Select(path => new GmvXEntry(
                Code: RequiredString(ReadObject(path), "code"),
                Category: new DirectoryInfo(Path.GetDirectoryName(path)!).Name))
            .ToArray();

        Assert.Equal(1308, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, entries.Length);
        Assert.Equal(121, entries.Count(entry => entry.Category == "outdoor"));
        Assert.Equal(60, entries.Count(entry => entry.Category == "indoor"));
        Assert.Equal(44, entries.Count(entry => entry.Category == "status"));
        Assert.Equal(38, entries.Count(entry => entry.Category == "debugging"));

        Assert.Equal(21, ReadStageEntries().Count);
        Assert.Equal(4, DetailedCodes.Length);
        Assert.Equal(17, TableOnlyCodes.Length);
    }

    [Fact]
    public void Gmv6RuntimeScopeRemainsClosedAndUntouched()
    {
        var gmv6Root = Path.Combine(GreeRoot, "gmv6");

        Assert.Equal(263, Directory.GetFiles(gmv6Root, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(gmv6Root, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(gmv6Root, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(gmv6Root, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(gmv6Root, "debugging"), "*.json").Length);
    }

    private static void AssertText(string code, string[] fragments)
    {
        var visible = VisibleBlob(ReadStageEntries()[code]);
        foreach (var fragment in fragments)
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

    private sealed record GmvXEntry(string Code, string Category);
}
