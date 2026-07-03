using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXTableOnlyOutdoorBatch1RepairTests
{
    private static readonly string GreeRoot = Path.Combine(TestPaths.RepoRoot, "data", "equipment-diagnostics", "error-knowledge", "gree");
    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string OutdoorRoot = Path.Combine(GmvXRoot, "outdoor");

    private static readonly string[] StageCodes =
    [
        "bb", "bE", "bF", "bH", "bP", "bU", "E0", "FP",
        "G0", "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9",
        "GA", "Gb", "GC", "Gd", "GE", "GF", "GH", "GJ", "GL", "Gn", "GP", "GU", "Gy"
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
        "замените плату",
        "замените датчик",
        "замените компрессор",
        "неисправна плата",
        "неисправен датчик",
        "неисправен компрессор"
    ];

    [Fact]
    public void AllStageCodesExistWithSafeGmvXTableOnlyText()
    {
        var entries = ReadEntries();

        Assert.Equal(31, entries.Count);
        Assert.Equal(StageCodes.Order(StringComparer.Ordinal), entries.Keys.Order(StringComparer.Ordinal));

        foreach (var (code, entry) in entries)
        {
            var visible = VisibleBlob(entry);
            Assert.Contains($"Gree GMV X — {code} —", visible, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV6", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree GMV —", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("пошаговая диагностика", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("подробная процедура", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var forbidden in ForbiddenVisibleFragments)
                Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);

            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
                Assert.Empty(RequiredArray(text, "possibleCauses"));
                var steps = string.Join(' ', RequiredArray(text, "checkSteps").OfType<JsonValue>().Select(value => value.GetValue<string>()));
                Assert.Contains($"Зафиксируйте код {code}", steps, StringComparison.Ordinal);
                Assert.Contains("наружной системе Gree GMV X", steps, StringComparison.Ordinal);
                Assert.Contains("сервисному инженеру", steps, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void RepresentativeMeaningsStayExactAndKeepPvGridScope()
    {
        AssertText("bb", ["датчика температуры возврата масла 4"]);
        AssertText("bE", ["датчика температуры входной трубки конденсатора"]);
        AssertText("E0", ["общую неисправность наружного блока"]);
        AssertText("FP", ["нарушение работы DC-двигателя"]);
        AssertText("G0", ["обратного подключения фотоэлектрической системы"]);
        AssertText("G1", ["островного режима"]);
        AssertText("G2", ["фотоэлектрической DC-цепи от сверхтока"]);
        AssertText("G3", ["перегрузку выработки мощности"]);
        AssertText("GJ", ["высокотемпературную защиту модуля на стороне электросети"]);
        AssertText("GL", ["аппаратную защиту от сверхтока на стороне электросети"]);
        AssertText("Gn", ["защиту по сопротивлению изоляции"]);
        AssertText("GP", ["защиту датчика температуры на стороне электросети"]);
        AssertText("GU", ["защиту цепи заряда"]);
        AssertText("Gy", ["защиту питания фотоэлектрической системы"]);
    }

    [Fact]
    public void RuntimeCountsRemainStableAndGmv6IsUntouched()
    {
        Assert.Equal(1296, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(OutdoorRoot, "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(GmvXRoot, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(GmvXRoot, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(GmvXRoot, "debugging"), "*.json").Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRoot, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
    }

    private static void AssertText(string code, string[] fragments)
    {
        var visible = VisibleBlob(ReadEntries()[code]);
        foreach (var fragment in fragments)
            Assert.Contains(fragment, visible, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, JsonObject> ReadEntries()
    {
        var codes = StageCodes.ToHashSet(StringComparer.Ordinal);
        return Directory.GetFiles(OutdoorRoot, "*.json")
            .Select(path => Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path))))
            .Where(entry => codes.Contains(RequiredString(entry, "code")))
            .ToDictionary(entry => RequiredString(entry, "code"), StringComparer.Ordinal);
    }

    private static string VisibleBlob(JsonObject entry)
    {
        var parts = new List<string>();
        foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
        {
            foreach (var property in new[] { "title", "summary", "recommendedAction", "safetyNote", "sourceNote" })
                parts.Add(RequiredString(text, property));
            foreach (var property in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
                parts.AddRange(RequiredArray(text, property).OfType<JsonValue>().Select(value => value.GetValue<string>()));
        }
        return string.Join('\n', parts);
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
