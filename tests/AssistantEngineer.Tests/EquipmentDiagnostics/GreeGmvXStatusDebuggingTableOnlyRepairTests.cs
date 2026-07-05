using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXStatusDebuggingTableOnlyRepairTests
{
    private static readonly string GreeRoot = Path.Combine(TestPaths.RepoRoot, "data", "equipment-diagnostics", "error-knowledge", "gree");
    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string StatusRoot = Path.Combine(GmvXRoot, "status");
    private static readonly string DebuggingRoot = Path.Combine(GmvXRoot, "debugging");

    private static readonly string[] StageCodes =
    [
        "A9", "AL", "An", "Ay",
        "n1", "n3", "n5", "nb", "nJ", "nn", "nU",
        "qA", "qC", "qH", "qP", "qU",
        "C1", "C7", "CU", "U5", "Ud", "Un", "Uy"
    ];

    private static readonly string[] StatusCodes =
    [
        "A9", "AL", "An", "Ay",
        "n1", "n3", "n5", "nb", "nJ", "nn", "nU",
        "qA", "qC", "qH", "qP", "qU"
    ];

    private static readonly string[] DebuggingCodes = ["C1", "C7", "CU", "U5", "Ud", "Un", "Uy"];

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
        "карточка неисправности",
        "авария",
        "аварию",
        "неисправность"
    ];

    [Fact]
    public void AllScopedStatusAndDebuggingCodesExistWithSafeGmvXText()
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
                Assert.Empty(RequiredArray(text, "possibleCauses"));
            }
        }
    }

    [Fact]
    public void StatusCodesRemainStatusModeOrSettingIndications()
    {
        foreach (var code in StatusCodes)
        {
            var visible = VisibleBlob(ReadStageEntries()[code]);
            Assert.True(
                visible.Contains("статус", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("режим", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("настрой", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("запрос", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("предотвращение", StringComparison.OrdinalIgnoreCase),
                $"{code} must stay status/mode/setting wording.");
        }

        AssertText("A9", ["Set Back"]);
        AssertText("AL", ["автоматической заправки хладагента"]);
        AssertText("An", ["блокировки от детей"]);
        AssertText("Ay", ["экранирования"]);
        AssertText("n1", ["цикла оттайки K1"]);
        AssertText("nJ", ["предотвращение перегрева при отоплении"]);
        AssertText("nU", ["удалённого экранирования внутреннего блока"]);
        AssertText("qA", ["рекуперации тепла"]);
        AssertText("qC", ["преимущественного охлаждения"]);
        AssertText("qH", ["преимущественного отопления"]);
        AssertText("qP", ["региона экспорта для PV VRF"]);
        AssertText("qU", ["напряжения электросети"]);
    }

    [Fact]
    public void DebuggingCodesRemainCommissioningOrServiceIndications()
    {
        foreach (var code in DebuggingCodes)
        {
            var visible = VisibleBlob(ReadStageEntries()[code]);
            Assert.True(
                visible.Contains("налад", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("сервис", StringComparison.OrdinalIgnoreCase) ||
                visible.Contains("защиту PV-модуля", StringComparison.OrdinalIgnoreCase),
                $"{code} must stay debugging/service-process wording.");
        }

        AssertText("C1", ["основным управлением", "DC-DC контроллером"]);
        AssertText("C7", ["связь преобразователя"]);
        AssertText("CU", ["внутренним блоком", "приёмной лампой"]);
        AssertText("U5", ["адрес платы привода компрессора"]);
        AssertText("Ud", ["плату подключения к электросети"]);
        AssertText("Un", ["платой подключения к электросети", "основной платой"]);
        AssertText("Uy", ["PV-модуля", "перегрева"]);
    }

    [Fact]
    public void RuntimeCountsRemainStableAndGmv6IsUntouched()
    {
        Assert.Equal(1308, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(GmvXRoot, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(GmvXRoot, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(StatusRoot, "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(DebuggingRoot, "*.json").Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRoot, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
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
        return Directory.GetFiles(StatusRoot, "*.json")
            .Concat(Directory.GetFiles(DebuggingRoot, "*.json"))
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
                parts.AddRange(RequiredArray(text, propertyName).OfType<JsonValue>().Select(value => value.GetValue<string>()));
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
