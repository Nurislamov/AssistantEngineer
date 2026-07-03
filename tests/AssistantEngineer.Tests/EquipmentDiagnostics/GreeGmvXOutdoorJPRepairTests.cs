using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXOutdoorJPRepairTests
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
        "J0", "J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9",
        "P0", "P1", "P2", "P3", "P5", "P6", "P7", "P8", "P9", "PC", "PH", "PJ", "PL"
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
    public void AllStageJpCodesExistAndAreRepaired()
    {
        var entries = ReadStageEntries();

        Assert.Equal(23, entries.Count);
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
    public void JMeaningsStaySpecificToOutdoorProcedures()
    {
        AssertText("J0", ["другой модуль", "исправно работающий модуль"]);
        AssertText("J1", ["компрессора 1", "320-460 V"]);
        AssertText("J6", ["компрессора 6", "320-460 V"]);
        AssertText("J7", ["четырехходового клапана", "0,1 MPa", "220 V"]);
        AssertText("J8", ["отношение давлений больше 8", "датчик давления"]);
        AssertText("J9", ["отношение давлений меньше 1,8", "датчик давления"]);
    }

    [Fact]
    public void PMeaningsStaySpecificToCompressorDriveProcedures()
    {
        AssertText("P0", ["P3", "P7", "P8", "PC", "PF", "P9", "PJ", "двухразрядном LED"]);
        AssertText("P1", ["P5", "P6", "C2", "двухразрядном LED"]);
        AssertText("P2", ["PH", "PL", "DC-шины"]);
        AssertText("P3", ["сбросовую защиту", "платы привода компрессора"]);
        AssertText("P5", ["сверхтоку", "2 Ом", "2 МОм", "UVW"]);
        AssertText("P6", ["IPM", "2 Ом", "2 МОм", "UVW"]);
        AssertText("P7", ["датчика температуры", "платы привода компрессора"]);
        AssertText("P8", ["IPM", "термопаст", "винтов"]);
        AssertText("P9", ["потери синхронизации", "2 Ом", "2 МОм"]);
        AssertText("PC", ["цепи детекции тока", "плату привода компрессора"]);
        AssertText("PH", ["выше 460 V", "380 V"]);
        AssertText("PL", ["ниже 320 V", "380 V"]);
        AssertText("PJ", ["неудачный запуск", "инверторного компрессора"]);
    }

    [Fact]
    public void StageJpRuntimeCountsRemainStable()
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
