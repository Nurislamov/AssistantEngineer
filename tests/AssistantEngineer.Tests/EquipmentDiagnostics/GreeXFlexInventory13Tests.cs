using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeXFlexInventory13Tests
{
    private static readonly string ReportDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "x-flex-inventory-13");

    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    [Fact]
    public void XFlexInventory13ReportArtifactsExist()
    {
        foreach (var fileName in new[]
        {
            "README.md",
            "source-manuals-inventory-13.csv",
            "source-manuals-inventory-13.json",
            "x-series-code-inventory-13.csv",
            "x-series-code-inventory-13.json",
            "flex-9-series-code-inventory-13.csv",
            "flex-9-series-code-inventory-13.json",
            "runtime-overlap-13.csv",
            "import-plan-draft-13.csv",
            "manual-review-13.csv"
        })
        {
            Assert.True(File.Exists(Path.Combine(ReportDirectory, fileName)), $"Missing report artifact: {fileName}");
        }
    }

    [Fact]
    public void SourceInventoryDocumentsXSourcesAndFlexSourceMissing()
    {
        var sources = ReadArray(Path.Combine(ReportDirectory, "source-manuals-inventory-13.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();

        Assert.Equal(3, sources.Length);

        var xOwner = Assert.Single(sources, row => RequiredString(row, "manualId") == "gree-gmv-x-owner-manual");
        Assert.Equal("X series", RequiredString(xOwner, "seriesGuess"));
        Assert.Equal("yes", RequiredString(xOwner, "fileExistsLocally"));
        Assert.Equal("yes", RequiredString(xOwner, "textCanBeExtracted"));
        Assert.Equal("yes", RequiredString(xOwner, "appearsToContainFaultTables"));

        var xSales = Assert.Single(sources, row => RequiredString(row, "manualId") == "gree-gmv-x-technical-sales-guide");
        Assert.Equal("reference-only", RequiredString(xSales, "inventoryAction"));
        Assert.Equal("no", RequiredString(xSales, "appearsToContainFaultTables"));

        var flex = Assert.Single(sources, row => RequiredString(row, "manualId") == "source-missing-gmv9-flex-service-manual");
        Assert.Equal("9 series Flex", RequiredString(flex, "seriesGuess"));
        Assert.Equal("no", RequiredString(flex, "fileExistsLocally"));
        Assert.Equal("source-missing", RequiredString(flex, "inventoryAction"));
    }

    [Fact]
    public void CodeInventoryIsReportOnlyAndKeepsAmbiguousRowsOutOfReadyImport()
    {
        var xRows = ReadArray(Path.Combine(ReportDirectory, "x-series-code-inventory-13.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();
        var flexRows = ReadArray(Path.Combine(ReportDirectory, "flex-9-series-code-inventory-13.json"));
        var review = ReadCsv(Path.Combine(ReportDirectory, "manual-review-13.csv"));

        Assert.Equal(63, xRows.Length);
        Assert.Empty(flexRows);
        Assert.Equal(11, CountCategory(xRows, "indoor"));
        Assert.Equal(36, CountCategory(xRows, "outdoor"));
        Assert.Equal(10, CountCategory(xRows, "status"));
        Assert.Equal(6, CountCategory(xRows, "debugging"));

        foreach (var row in xRows)
        {
            Assert.Equal("X series", RequiredString(row, "seriesCandidate"));
            Assert.Equal("gree-gmv-x-owner-manual", RequiredString(row, "sourceManualId"));
            Assert.Equal("needs-manual-review", RequiredString(row, "actionRecommendation"));
            Assert.NotEmpty(RequiredString(row, "sourcePath"));
            Assert.NotEmpty(RequiredString(row, "sourceMeaningEn"));
            Assert.NotEqual("ready-for-import", RequiredString(row, "actionRecommendation"));
        }

        Assert.Equal(63, review.Count(row => row["actionRecommendation"] == "needs-manual-review"));
        Assert.Contains(review, row =>
            row["seriesCandidate"] == "9 series Flex" &&
            row["actionRecommendation"] == "source-missing");
        Assert.Contains(review, row =>
            row["sourceManualId"] == "gree-gmv-x-technical-sales-guide" &&
            row["actionRecommendation"] == "not-a-diagnostic-code");
    }

    [Fact]
    public void RuntimeCountsReflectLaterGmvXAndGmv9FlexImportsAndAlternateFlexFoldersRemainAbsent()
    {
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(136, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-mini"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-x"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(260, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(922, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        foreach (var folder in new[] { "x-series", "9-series-flex", "flex" })
        {
            Assert.False(Directory.Exists(Path.Combine(GreeRuntimeDirectory, folder)), $"Alternate X/Flex runtime folder must not exist: {folder}");
        }

        var packageDirectory = Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "packages");
        var packageNames = Directory.GetFiles(packageDirectory, "*.json")
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.Contains(packageNames, name => name.Contains("gmv-x", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(packageNames, name => name.Contains("gmv9-flex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RuntimeOverlapDocumentsOverlapWithoutPromotingRows()
    {
        var overlap = ReadCsv(Path.Combine(ReportDirectory, "runtime-overlap-13.csv"));

        Assert.Equal(63, overlap.Count);
        Assert.Contains(overlap, row =>
            row["canonicalCode"] == "E0" &&
            row["alreadyInRuntime"] == "yes" &&
            row["overlapNote"].Contains("do not reuse meaning across series", StringComparison.Ordinal));
        Assert.Contains(overlap, row =>
            row["canonicalCode"] == "QA" &&
            row["alreadyInRuntime"] == "no");
    }

    private static int CountCategory(IReadOnlyCollection<JsonObject> rows, string category) =>
        rows.Count(row => RequiredString(row, "categoryCandidate") == category);

    private static JsonArray ReadArray(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);

        return Assert.IsType<JsonArray>(node);
    }

    private static string RequiredString(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);

        var value = node.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(value), $"Property {propertyName} must not be blank.");

        return value;
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
}
