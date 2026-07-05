using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXTableOnlyOutdoorBatch2RepairTests
{
    private static readonly string GreeRoot = Path.Combine(TestPaths.RepoRoot, "data", "equipment-diagnostics", "error-knowledge", "gree");
    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string OutdoorRoot = Path.Combine(GmvXRoot, "outdoor");

    private static readonly string[] StageCodes =
    [
        "H4", "HA", "HE", "HF", "HP", "HU",
        "JA", "JC", "JE", "JF", "JL",
        "P4", "PA", "PE", "PF", "PP", "PU"
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

        Assert.Equal(17, entries.Count);
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
    public void RepresentativeMeaningsKeepFanPressureAndCompressorScopeSeparate()
    {
        AssertText("H4", ["защита PFC привода вентилятора"]);
        AssertText("HA", ["микросхемы памяти привода наружного инверторного вентилятора"]);
        AssertText("HU", ["входному AC-напряжению привода инверторного вентилятора"]);
        AssertText("JA", ["защиту системы при ненормальном давлении"]);
        AssertText("JL", ["значение высокого давления слишком низкое"]);
        AssertText("P4", ["защита PFC привода компрессора"]);
        AssertText("PA", ["микросхемы памяти привода компрессора"]);
        AssertText("PU", ["входному AC-напряжению привода инверторного компрессора"]);
    }

    [Fact]
    public void RuntimeCountsRemainStableAndGmv6IsUntouched()
    {
        Assert.Equal(1308, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
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
