using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeErvQuality1Tests
{
    private static readonly string RuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "erv-b-series",
        "system");

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedMeanings =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["dF"] = ["температурного датчика", "проводного контроллера"],
            ["dH"] = ["электрической цепи", "проводного контроллера"],
            ["E6"] = ["связи", "проводным контроллером", "блоком ERV"],
            ["L0"] = ["воздушной заслонке", "приводу", "воздушным клапаном"],
            ["L9"] = ["количество блоков", "групповом управлении"]
        };

    [Fact]
    public void AllFiveErvCardsRemainManualAlignedAndUserFacingTextIsClean()
    {
        var files = Directory.GetFiles(RuntimeDirectory, "*.json");
        Assert.Equal(5, files.Length);

        foreach (var (code, expectedFragments) in ExpectedMeanings)
        {
            var entry = ReadObject(Path.Combine(RuntimeDirectory, $"{code.ToLowerInvariant()}.json"));
            Assert.Equal(code, RequiredString(entry, "code"));
            Assert.Equal("ERV B Series", RequiredString(entry, "series"));
            Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));

            var visible = string.Join(
                " ",
                RequiredArray(entry, "texts")
                    .OfType<JsonObject>()
                    .SelectMany(VisibleValues));

            Assert.All(expectedFragments, fragment =>
                Assert.Contains(fragment, visible, StringComparison.OrdinalIgnoreCase));
            Assert.All(
                new[]
                {
                    "Подтвердите код",
                    "карточка",
                    "карточка неисправности",
                    "manual",
                    "sourceNote",
                    "packageId",
                    "по таблице",
                    "основание",
                    "руководство"
                },
                fragment => Assert.DoesNotContain(fragment, visible, StringComparison.OrdinalIgnoreCase));
        }
    }

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
