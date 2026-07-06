using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmv6HrQuality1Tests
{
    private static readonly string RuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv6-hr");

    [Fact]
    public void AllGmv6HrCardsRemainInExpectedSectionsAndUserFacingTextIsClean()
    {
        Assert.Equal(38, Count("debugging"));
        Assert.Equal(60, Count("indoor"));
        Assert.Equal(120, Count("outdoor"));
        Assert.Equal(44, Count("status"));
        Assert.Equal(262, Directory.GetFiles(RuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        foreach (var path in Directory.GetFiles(RuntimeDirectory, "*.json", SearchOption.AllDirectories))
        {
            var entry = ReadObject(path);
            Assert.Equal("GMV6 HR", RequiredString(entry, "series"));

            var visible = string.Join(
                " ",
                RequiredArray(entry, "texts")
                    .OfType<JsonObject>()
                    .SelectMany(VisibleValues));
            Assert.All(
                new[]
                {
                    "Подтвердите код",
                    "Сверьте модель",
                    "карточка",
                    "карточка неисправности",
                    "manual",
                    "sourceNote",
                    "packageId",
                    "руководство",
                    "основание"
                },
                fragment => Assert.DoesNotContain(fragment, visible, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void StatusQueryAndFunctionSettingCardsAreNotPresentedAsComponentFailures()
    {
        var files = Directory.GetFiles(Path.Combine(RuntimeDirectory, "status"), "*.json");
        Assert.Equal(44, files.Length);

        foreach (var path in files)
        {
            var entry = ReadObject(path);
            Assert.Equal("Status", RequiredString(entry, "signalType"));
            Assert.Equal("Info", RequiredString(entry, "severity"));

            var texts = RequiredArray(entry, "texts").OfType<JsonObject>().ToArray();
            Assert.Contains(
                "не самостоятельный признак отказа компонента",
                RequiredString(texts[0], "summary"),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static int Count(string category) =>
        Directory.GetFiles(Path.Combine(RuntimeDirectory, category), "*.json").Length;

    private static IEnumerable<string> VisibleValues(JsonObject text)
    {
        foreach (var property in new[] { "title", "summary", "safetyNote", "recommendedAction", "sourceNote" })
            yield return RequiredString(text, property);

        foreach (var property in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
        {
            foreach (var value in RequiredArray(text, property))
                yield return Assert.IsAssignableFrom<JsonValue>(value).GetValue<string>();
        }
    }

    private static JsonObject ReadObject(string path) =>
        Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));

    private static JsonArray RequiredArray(JsonObject node, string property) =>
        Assert.IsType<JsonArray>(node[property]);

    private static string RequiredString(JsonObject node, string property) =>
        Assert.IsAssignableFrom<JsonValue>(node[property]).GetValue<string>();
}
