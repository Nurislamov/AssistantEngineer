using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXQuality1Tests
{
    private static readonly string RuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv-x");

    private static readonly IReadOnlyDictionary<string, string[]> AffectedCodes =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["debugging"] = ["C1", "C7", "CU", "U5", "Ud", "Un", "Uy"],
            ["indoor"] = ["dy", "L8", "Lb", "LE", "LJ", "LP", "o0", "o1", "o2", "o4", "o5", "o6", "oA", "ob", "oC", "y1", "y2"]
        };

    [Fact]
    public void AllTwentyFourAffectedGmvXCardsAreUserFacingCleanWithoutMetadataDrift()
    {
        Assert.Equal(24, AffectedCodes.Sum(group => group.Value.Length));
        Assert.Equal(263, Directory.GetFiles(RuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        foreach (var (category, codes) in AffectedCodes)
        {
            foreach (var code in codes)
            {
                var entry = ReadObject(Path.Combine(RuntimeDirectory, category, $"{code.ToLowerInvariant()}.json"));
                Assert.Equal(code, RequiredString(entry, "code"));
                Assert.Equal("GMV X", RequiredString(entry, "series"));
                var expectedPackageId = category == "indoor"
                    ? "gree-gmv-x-indoor-fault-codes"
                    : "gree-gmv-x-debugging-codes";
                Assert.Equal(expectedPackageId, RequiredString(entry, "packageId"));
                Assert.False(string.IsNullOrWhiteSpace(RequiredString(entry, "sourceMeaning")));

                var visible = string.Join(
                    " ",
                    RequiredArray(entry, "texts")
                        .OfType<JsonObject>()
                        .SelectMany(VisibleValues));
                Assert.DoesNotContain("карточка", visible, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("карточка неисправности", visible, StringComparison.OrdinalIgnoreCase);
            }
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
