using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeManualBoundCardRepairTests
{
    private static readonly string GreeRoot = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string[] TelegramVisibleFields =
    [
        "title",
        "summary",
        "possibleCauses",
        "checkSteps",
        "recommendedAction",
        "safetyNote"
    ];

    private static readonly string[] ForbiddenVisiblePhrases =
    [
        "по таблице руководства",
        "классифицирован по таблице",
        "источник",
        "manual",
        "source",
        "document code",
        "manualId",
        "sourceReference",
        "packageId",
        "sourceMeaning",
        "текущая карточка",
        "диагностический вывод должен оставаться",
        "точная исходная формулировка",
        "не расширяйте трактовку",
        "дальнейшие действия выполните по сервисной процедуре"
    ];

    [Fact]
    public void TelegramVisibleGreeTextsDoNotExposeImportOrProvenanceWording()
    {
        foreach (var (path, entry) in ReadEntries())
        {
            var visible = VisibleBlob(entry);

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.False(
                string.IsNullOrWhiteSpace(visible),
                $"Visible Telegram text is empty: {path}");
        }
    }

    [Fact]
    public void Gmv6AjIsAFilterCleaningPromptWithTheDocumentedAction()
    {
        var entry = ReadEntry("gmv6", "status", "aj.json");
        var visible = VisibleBlob(entry);

        Assert.Contains("чистк", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("фильтр", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("статус", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("сброс", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("сервисный цикл", visible, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "напоминание по обслуживанию оборудования",
            visible,
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(RequiredArray(RequiredTexts(entry)[0], "possibleCauses"));
    }

    [Fact]
    public void Gmv6B1ContainsTheDocumentedDiagnosisCausesAndFlowchart()
    {
        var entry = ReadEntry("gmv6", "outdoor", "b1.json");
        var visible = VisibleBlob(entry);

        Assert.Contains(
            "датчика температуры наружного воздуха",
            visible,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("30 секунд", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("плохой контакт", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("разъём", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("датчик температуры", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("цепь детекции", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("основную плату", visible, StringComparison.OrdinalIgnoreCase);

        var technicalTexts = RequiredTexts(entry)
            .Where(text =>
                !string.Equals(
                    RequiredString(text, "audience"),
                    "Consumer",
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(technicalTexts);
        Assert.All(
            technicalTexts,
            text => Assert.True(
                RequiredArray(text, "checkSteps").Count >= 3,
                $"Audience {RequiredString(text, "audience")} must retain all flowchart steps."));
    }

    [Fact]
    public void GreeRuntimeAndSeriesCountsRemainStable()
    {
        var entries = ReadEntries().Select(item => item.Entry).ToArray();
        var expectedSeriesCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["GMV6 HR"] = 262,
            ["GMV6"] = 263,
            ["GMV Mini"] = 136,
            ["GMV X"] = 263,
            ["GMV9 Flex"] = 260,
            ["U-Match R32"] = 107,
            ["ERV B Series"] = 5
        };

        Assert.Equal(1296, entries.Length);
        foreach (var (series, expectedCount) in expectedSeriesCounts)
        {
            Assert.Equal(
                expectedCount,
                entries.Count(entry =>
                    string.Equals(
                        RequiredString(entry, "series"),
                        series,
                        StringComparison.Ordinal)));
        }
    }

    private static IEnumerable<(string Path, JsonObject Entry)> ReadEntries() =>
        Directory
            .EnumerateFiles(GreeRoot, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => (path, ReadObject(path)));

    private static JsonObject ReadEntry(params string[] pathSegments) =>
        ReadObject(Path.Combine(new[] { GreeRoot }.Concat(pathSegments).ToArray()));

    private static JsonObject ReadObject(string path)
    {
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);
        return Assert.IsType<JsonObject>(node);
    }

    private static JsonObject[] RequiredTexts(JsonObject entry) =>
        RequiredArray(entry, "texts")
            .Select(node => Assert.IsType<JsonObject>(node))
            .ToArray();

    private static string VisibleBlob(JsonObject entry)
    {
        var values = new List<string>();
        foreach (var text in RequiredTexts(entry))
        {
            foreach (var field in TelegramVisibleFields)
            {
                var node = text[field];
                switch (node)
                {
                    case JsonValue value:
                        values.Add(value.GetValue<string>());
                        break;
                    case JsonArray array:
                        values.AddRange(array.Select(item => item!.GetValue<string>()));
                        break;
                }
            }
        }

        return string.Join("\n", values);
    }

    private static JsonArray RequiredArray(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return Assert.IsType<JsonArray>(node);
    }

    private static string RequiredString(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return node.GetValue<string>();
    }
}
