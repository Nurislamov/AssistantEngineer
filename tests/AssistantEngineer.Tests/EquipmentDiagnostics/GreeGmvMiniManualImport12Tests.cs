using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvMiniManualImport12Tests
{
    private static readonly string ReportDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "gmv-mini-manual-import-12");

    private static readonly string MiniRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv-mini");

    [Fact]
    public void GmvMiniManualImport12ReportArtifactsExist()
    {
        foreach (var fileName in new[]
        {
            "README.md",
            "gmv-mini-manual-code-inventory-12.csv",
            "gmv-mini-manual-code-inventory-12.json",
            "gmv-mini-runtime-add-plan-12.csv",
            "gmv-mini-manual-review-12.csv"
        })
        {
            Assert.True(File.Exists(Path.Combine(ReportDirectory, fileName)), $"Missing report artifact: {fileName}");
        }
    }

    [Fact]
    public void GmvMiniManualInventoryDocumentsAddsAliasesAndE6Review()
    {
        var inventory = ReadArray(Path.Combine(ReportDirectory, "gmv-mini-manual-code-inventory-12.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();
        var addPlan = ReadCsv(Path.Combine(ReportDirectory, "gmv-mini-runtime-add-plan-12.csv"));
        var review = ReadCsv(Path.Combine(ReportDirectory, "gmv-mini-manual-review-12.csv"));

        Assert.Equal(138, inventory.Length);
        Assert.Equal(127, addPlan.Count);
        Assert.Equal(9, inventory.Count(row => RequiredString(row, "action") == "keep-existing"));
        Assert.Equal(2, inventory.Count(row => RequiredString(row, "action") == "alias-to-existing"));

        Assert.Contains(inventory, row =>
            RequiredString(row, "code") == "C0" &&
            RequiredString(row, "sectionNumber") == "2.2" &&
            RequiredString(row, "action") == "alias-to-existing");
        Assert.Contains(inventory, row =>
            RequiredString(row, "code") == "AJ" &&
            RequiredString(row, "sectionNumber") == "2.2" &&
            RequiredString(row, "action") == "alias-to-existing");

        var e6 = Assert.Single(review);
        Assert.Equal("E6", e6["code"]);
        Assert.Equal("manual-review", e6["action"]);
        Assert.Contains("did not contain an exact GMV Mini E6", e6["reason"], StringComparison.Ordinal);
    }

    [Fact]
    public void GmvMiniRuntimeAndPackagesMatchFullImportWithoutE6()
    {
        AssertPackageCount("gree-gmv-mini-vrf-indoor-controller-codes.json", "indoor", 27);
        AssertPackageCount("gree-gmv-mini-vrf-outdoor-protection-codes.json", "outdoor", 62);
        AssertPackageCount("gree-gmv-mini-vrf-status-codes.json", "status", 47);

        var runtimeFiles = Directory.GetFiles(MiniRuntimeDirectory, "*.json", SearchOption.AllDirectories);
        Assert.Equal(136, runtimeFiles.Length);
        Assert.False(File.Exists(Path.Combine(MiniRuntimeDirectory, "indoor", "e6.json")));
        Assert.False(File.Exists(Path.Combine(MiniRuntimeDirectory, "outdoor", "e6.json")));
        Assert.False(File.Exists(Path.Combine(MiniRuntimeDirectory, "status", "e6.json")));

        Assert.True(File.Exists(Path.Combine(MiniRuntimeDirectory, "outdoor", "e0.json")));
        Assert.True(File.Exists(Path.Combine(MiniRuntimeDirectory, "indoor", "l0.json")));
        Assert.True(File.Exists(Path.Combine(MiniRuntimeDirectory, "status", "01.json")));
    }

    [Fact]
    public void EmbeddedRuntimeCatalogExposesAllGmvMiniEntriesToBotSearch()
    {
        var entries = new JsonErrorKnowledgeLocalizationSource().GetEntries();
        var miniEntries = entries
            .Where(entry => string.Equals(entry.Manufacturer, "Gree", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(entry.Series, "GMV Mini", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Equal(1293, entries.Count);
        Assert.Equal(136, miniEntries.Length);

        foreach (var (code, id) in new[]
        {
            ("d1", "gree-gmv-mini-indoor-d1"),
            ("b1", "gree-gmv-mini-outdoor-b1"),
            ("E0", "gree-gmv-mini-outdoor-e0"),
            ("P0", "gree-gmv-mini-outdoor-p0"),
            ("01", "gree-gmv-mini-status-01"),
            ("n2", "gree-gmv-mini-status-n2")
        })
        {
            var entry = Assert.Single(miniEntries, item =>
                item.Id == id &&
                item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            Assert.True(EquipmentDiagnosticBotReferencePolicy.IsSearchableLocalizedEntry(entry));
            Assert.NotEmpty(entry.SourceReferences);
            Assert.DoesNotContain(entry.Texts, text =>
                text.Title.Contains("неисправность for", StringComparison.OrdinalIgnoreCase) ||
                text.Title.Contains("неисправность of", StringComparison.OrdinalIgnoreCase) ||
                text.Title.Contains("driven board for", StringComparison.OrdinalIgnoreCase) ||
                text.Summary.Contains("неисправность for", StringComparison.OrdinalIgnoreCase) ||
                text.Summary.Contains("неисправность of", StringComparison.OrdinalIgnoreCase) ||
                text.Summary.Contains("driven board for", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void AssertPackageCount(string packageFileName, string folder, int expectedCount)
    {
        var package = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "packages",
            packageFileName));
        var actualCount = Directory.GetFiles(Path.Combine(MiniRuntimeDirectory, folder), "*.json").Length;

        Assert.Equal(expectedCount, actualCount);
        Assert.Equal(expectedCount, RequiredInt(package, "entryCountExpected"));
    }

    private static JsonObject ReadObject(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);

        return Assert.IsType<JsonObject>(node);
    }

    private static JsonArray ReadArray(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);

        return Assert.IsType<JsonArray>(node);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadCsv(string path)
    {
        Assert.True(File.Exists(path), $"CSV file does not exist: {path}");
        var lines = File.ReadAllLines(path);
        Assert.NotEmpty(lines);

        var headers = ParseCsvLine(lines[0]);
        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseCsvLine)
            .Select(values =>
            {
                Assert.Equal(headers.Length, values.Length);
                return (IReadOnlyDictionary<string, string>)headers
                    .Select((header, index) => (header, value: values[index]))
                    .ToDictionary(item => item.header, item => item.value, StringComparer.Ordinal);
            })
            .ToArray();
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString());
        return values.ToArray();
    }

    private static string RequiredString(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);

        var value = node.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(value), $"Property '{propertyName}' must not be empty.");

        return value;
    }

    private static int RequiredInt(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);

        return node.GetValue<int>();
    }
}
