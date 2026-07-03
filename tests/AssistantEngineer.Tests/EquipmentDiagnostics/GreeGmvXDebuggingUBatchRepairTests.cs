using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXDebuggingUBatchRepairTests
{
    private static readonly string GreeRoot = Path.Combine(TestPaths.RepoRoot, "data", "equipment-diagnostics", "error-knowledge", "gree");
    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string DebuggingRoot = Path.Combine(GmvXRoot, "debugging");

    private static readonly string[] StageCodes = ["U0", "U2", "U3", "U4", "U6", "U8", "U9", "UE", "UF", "UL"];

    private static readonly string[] ForbiddenVisibleFragments =
    [
        "карточка неисправности",
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
        Assert.Equal(10, entries.Count);
        Assert.Equal(StageCodes.Order(StringComparer.Ordinal), entries.Keys.Order(StringComparer.Ordinal));

        foreach (var (code, entry) in entries)
        {
            var visible = VisibleBlob(entry);
            Assert.Contains($"Gree GMV X — {code} —", visible, StringComparison.Ordinal);
            Assert.True(
                visible.Contains("налад", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("сервисн", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("подготовке к запуску", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("автоматическая заправка", StringComparison.OrdinalIgnoreCase),
                $"{code} must retain commissioning/debugging/service-process context.");
            Assert.DoesNotContain("Gree GMV6", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree GMV —", visible, StringComparison.OrdinalIgnoreCase);
            foreach (var forbidden in ForbiddenVisibleFragments)
                Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void UMeaningsAndProceduresStaySpecific()
    {
        AssertText("U0", ["прогревалось менее восьми часов", "более восьми часов", "штатный запуск"]);
        AssertText("U2", ["DIP-переключателя мощности", "перемычки", "фактическим наружным блоком"]);
        AssertText("U3", ["потерю фазы", "обратную последовательность фаз", "трёх фаз"]);
        AssertText("U4", ["показания высокого и низкого давления", "недостаток хладагента", "утечки"]);
        AssertText("U6", ["при наладке", "запорный клапан", "SW4"]);
        AssertText("U8", ["температура труб внутреннего блока", "катушки электронного расширительного клапана", "засор из-за пайки"]);
        AssertText("U9", ["давление системы", "трубах наружного блока", "загрязнение фильтра"]);
        AssertText("UE", ["от 0 до 40 °C", "автоматической заправки", "Заправку вручную"]);
        AssertText("UF", ["распределителе режимов", "несовместимость", "подключайте их"]);
        AssertText("UL", ["DIP-переключатель", "экстренного режима компрессора", "допустимого диапазона"]);
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
