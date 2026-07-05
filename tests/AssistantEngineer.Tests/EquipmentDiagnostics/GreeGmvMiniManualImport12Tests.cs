using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvMiniManualImport12Tests
{
    private const string DocumentCode = "GC202510-XIX";
    private const string ManualId = "gree-gmv-mini-service-manual";

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
        AssertPackageCount("gree-gmv-mini-vrf-status-codes.json", "status", 59);

        var runtimeFiles = Directory.GetFiles(MiniRuntimeDirectory, "*.json", SearchOption.AllDirectories);
        Assert.Equal(148, runtimeFiles.Length);
        Assert.False(File.Exists(Path.Combine(MiniRuntimeDirectory, "indoor", "e6.json")));
        Assert.False(File.Exists(Path.Combine(MiniRuntimeDirectory, "outdoor", "e6.json")));
        Assert.False(File.Exists(Path.Combine(MiniRuntimeDirectory, "status", "e6.json")));

        Assert.True(File.Exists(Path.Combine(MiniRuntimeDirectory, "outdoor", "e0.json")));
        Assert.True(File.Exists(Path.Combine(MiniRuntimeDirectory, "indoor", "l0.json")));
        Assert.True(File.Exists(Path.Combine(MiniRuntimeDirectory, "status", "01.json")));

        var greeRuntimeDirectory = Directory.GetParent(MiniRuntimeDirectory)!.FullName;
        Assert.Equal(263, Directory.GetFiles(Path.Combine(greeRuntimeDirectory, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(greeRuntimeDirectory, "gmv-x"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(260, Directory.GetFiles(Path.Combine(greeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void EmbeddedRuntimeCatalogExposesAllGmvMiniEntriesToBotSearch()
    {
        var entries = new JsonErrorKnowledgeLocalizationSource().GetEntries();
        var miniEntries = entries
            .Where(entry => string.Equals(entry.Manufacturer, "Gree", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(entry.Series, "GMV Mini", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Equal(1308, entries.Count);
        Assert.Equal(148, miniEntries.Length);

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

    [Fact]
    public void GmvMiniCardsUseReviewedManualIdentityAndCompleteModelScope()
    {
        var expectedModels = new[]
        {
            "GMV-141WL/C-T",
            "GMV-180WL/C-X(D)",
            "GMV-280WL/C1-X",
            "GMV-335WL/C1-X",
            "GMV-280WL/C1-X(S)",
            "GMV-335WL/C1-X(S)"
        };
        var entries = Directory
            .GetFiles(MiniRuntimeDirectory, "*.json", SearchOption.AllDirectories)
            .Select(ReadObject)
            .ToArray();

        Assert.Equal(148, entries.Length);
        Assert.All(entries, entry =>
        {
            Assert.Equal("GMV Mini", RequiredString(entry, "series"));
            Assert.Equal("Manual", RequiredString(entry, "sourceType"));
            Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
            Assert.Equal("High", RequiredString(entry, "confidence"));
            Assert.All(RequiredArray(entry, "sourceReferences").OfType<JsonObject>(), reference =>
            {
                Assert.Equal(ManualId, RequiredString(reference, "manualId"));
                Assert.Equal(DocumentCode, RequiredString(reference, "documentCode"));
            });
        });

        foreach (var model in expectedModels)
        {
            Assert.Contains(entries, entry =>
                RequiredArray(entry, "models")
                    .Select(node => node!.GetValue<string>())
                    .Contains(model, StringComparer.Ordinal));
        }
    }

    [Theory]
    [InlineData("qd")]
    [InlineData("n3")]
    [InlineData("n5")]
    [InlineData("nL")]
    [InlineData("nU")]
    [InlineData("q7")]
    [InlineData("q8")]
    [InlineData("q9")]
    [InlineData("qF")]
    [InlineData("qL")]
    [InlineData("qn")]
    [InlineData("qU")]
    public void Gmv141FunctionSettingCodesAreModelSpecificStatusCards(string code)
    {
        var entry = ReadObject(Path.Combine(
            MiniRuntimeDirectory,
            "status",
            $"{code.ToLowerInvariant()}.json"));

        Assert.Equal(code, RequiredString(entry, "code"));
        Assert.Equal("Status", RequiredString(entry, "signalType"));
        Assert.Equal("Info", RequiredString(entry, "severity"));
        Assert.Equal(["GMV-141WL/C-T"], RequiredArray(entry, "models").Select(node => node!.GetValue<string>()));
        Assert.Contains("Function setting", RequiredString(entry, "sourceReference"), StringComparison.Ordinal);
        Assert.All(RequiredArray(entry, "texts").OfType<JsonObject>(), text =>
        {
            Assert.Contains("сервисную настройку", RequiredString(text, "summary"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("авар", RequiredString(text, "summary"), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData("A0")]
    [InlineData("A2")]
    [InlineData("A6")]
    [InlineData("A7")]
    [InlineData("A8")]
    [InlineData("n2")]
    [InlineData("nH")]
    [InlineData("qF")]
    [InlineData("qL")]
    public void StatusAndFunctionCardsRemainNeutral(string code)
    {
        var entry = ReadObject(Path.Combine(
            MiniRuntimeDirectory,
            "status",
            $"{code.ToLowerInvariant()}.json"));

        Assert.Contains(
            RequiredString(entry, "signalType"),
            new[] { "Status", "Debug", "Commissioning" });
        Assert.All(RequiredArray(entry, "texts").OfType<JsonObject>(), text =>
        {
            var summary = RequiredString(text, "summary");
            Assert.DoesNotContain("авария", summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("поломка", summary, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData("indoor", "c0.json", "Communication malfunction")]
    [InlineData("outdoor", "e1.json", "High pressure protection")]
    [InlineData("outdoor", "e3.json", "Low pressure protection")]
    [InlineData("outdoor", "e4.json", "Discharge temperature protection")]
    [InlineData("indoor", "d3.json", "Temperature sensor malfunction")]
    [InlineData("outdoor", "f1.json", "Pressure sensor malfunction")]
    [InlineData("outdoor", "p8.json", "IPM overtemperature")]
    [InlineData("outdoor", "pf.json", "charging loop")]
    [InlineData("outdoor", "ph.json", "DC bus high-voltage")]
    [InlineData("outdoor", "c2.json", "inverter driver communication")]
    public void PracticalCardsReferenceMatchingTroubleshootingGroup(
        string category,
        string fileName,
        string expectedReference)
    {
        var entry = ReadObject(Path.Combine(MiniRuntimeDirectory, category, fileName));
        var references = RequiredArray(entry, "sourceReferences")
            .OfType<JsonObject>()
            .Select(reference => RequiredString(reference, "sourceReference"))
            .ToArray();

        Assert.Contains(references, reference =>
            reference.Contains(expectedReference, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EquipmentCatalogMarksMiniImportedAndListsReviewedModels()
    {
        var catalog = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "equipment-catalog",
            "gree-vrf-equipment-map.json"));
        var series = RequiredArray(catalog, "series").OfType<JsonObject>().ToArray();
        var mini = Assert.Single(series, entry => RequiredString(entry, "id") == "gmv5_mini");
        var aliases = RequiredArray(mini, "aliases").Select(node => node!.GetValue<string>()).ToArray();

        Assert.Equal("Imported", RequiredString(mini, "coverageStatus"));
        Assert.Contains("GMV-141WL/C-T", aliases);
        Assert.Contains("GMV-180WL/C-X(D)", aliases);
        Assert.Contains("GMV-280WL/C1-X", aliases);
        Assert.Contains("GMV-335WL/C1-X(S)", aliases);
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

    private static JsonArray RequiredArray(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return Assert.IsType<JsonArray>(node);
    }
}
