using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXDebuggingCBatchRepairTests
{
    private static readonly string GreeRoot = Path.Combine(TestPaths.RepoRoot, "data", "equipment-diagnostics", "error-knowledge", "gree");
    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string DebuggingRoot = Path.Combine(GmvXRoot, "debugging");

    private static readonly string[] StageCodes =
    [
        "C0", "C2", "C3", "C4", "C5", "C6", "Cb", "CC", "Cd", "CE", "CF", "CH", "CJ", "CL", "Cn", "CP", "Cy"
    ];

    private static readonly string[] ForbiddenVisibleFragments =
    [
        "карточка неисправности",
        "если есть авария",
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
    public void AllStageCodesExistAndUseCommissioningOrServiceWording()
    {
        var entries = ReadEntries();
        Assert.Equal(17, entries.Count);
        Assert.Equal(StageCodes.Order(StringComparer.Ordinal), entries.Keys.Order(StringComparer.Ordinal));

        foreach (var (code, entry) in entries)
        {
            var visible = VisibleBlob(entry);
            Assert.Contains($"Gree GMV X — {code} —", visible, StringComparison.Ordinal);
            Assert.True(
                visible.Contains("налад", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("сервисн", StringComparison.OrdinalIgnoreCase),
                $"{code} must be presented as commissioning/debugging/service-process diagnostics.");
            Assert.DoesNotContain("Gree GMV6", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree GMV —", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var forbidden in ForbiddenVisibleFragments)
                Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CMeaningsAndProceduresStaySpecific()
    {
        AssertText("C0", ["30 секунд", "не более двух пультов", "разные адреса"]);
        AssertText("C2", ["привод инверторного компрессора", "30 секунд"]);
        AssertText("C3", ["привод инверторного вентилятора", "30 секунд"]);
        AssertText("C4", ["более трёх внутренних блоков", "показывающих C0"]);
        AssertText("C5", ["средствами наладки и мониторинга", "один инженерный номер", "кнопку Reset"]);
        AssertText("C6", ["текущее число наружных модулей", "предыдущей наладке"]);
        AssertText("CH", ["1,35", "135%"]);
        AssertText("CL", ["0,5", "50%"]);
        AssertText("CC", ["SA8", "ровно один главный модуль"]);
        AssertText("CE", ["распределителя режимов", "в течение одной минуты", "внутренним блоком"]);
        AssertText("CF", ["SA8", "несколько главных"]);
        AssertText("CJ", ["CAN2", "SA2 = 00000"]);
        AssertText("CP", ["сети HBS", "параметре P13", "адресу 02"]);
        AssertText("Cb", ["более четырёх", "более 80", "до 100"]);
        AssertText("Cd", ["распределителя режимов", "наружным блоком", "одной минуты"]);
        AssertText("Cn", ["в течение 5 секунд", "соответствующим портам"]);
        AssertText("Cy", ["клапана переохлаждения", "одну минуту", "нескольких плат"]);
    }

    [Fact]
    public void RuntimeCountsRemainStableAndGmv6IsUntouched()
    {
        Assert.Equal(1296, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(GmvXRoot, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(GmvXRoot, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(GmvXRoot, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(DebuggingRoot, "*.json").Length);
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
        return Directory.GetFiles(DebuggingRoot, "*.json")
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
