using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmv6ManualImport11Tests
{
    private const string ManualId = "gree-gmv6-service-manual-2020-09";
    private const string ManualName = "Service Manual for GMV6 v_2020.09";
    private const string N2Meaning = "настройка предела коэффициента соответствия внутренних и наружных блоков";

    private static readonly string ReportDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "gmv6-manual-import-11");

    private static readonly string Gmv6RuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv6");

    [Fact]
    public void Gmv6ManualImport11ReportArtifactsExist()
    {
        foreach (var fileName in new[]
        {
            "README.md",
            "gmv6-manual-code-inventory-11.csv",
            "gmv6-manual-code-inventory-11.json",
            "gmv6-runtime-add-plan-11.csv",
            "gmv6-manual-review-11.csv"
        })
        {
            Assert.True(File.Exists(Path.Combine(ReportDirectory, fileName)), $"Missing report artifact: {fileName}");
        }
    }

    [Fact]
    public void Gmv6ManualInventoryMatchesRuntimeWithoutMissingAdds()
    {
        var inventory = ReadArray(Path.Combine(ReportDirectory, "gmv6-manual-code-inventory-11.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();
        var runtimeFiles = Directory.GetFiles(Gmv6RuntimeDirectory, "*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(TestPaths.RepoRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(255, inventory.Length);
        Assert.Equal(121, CountSide(inventory, "outdoor"));
        Assert.Equal(60, CountSide(inventory, "indoor"));
        Assert.Equal(37, CountSide(inventory, "status"));
        Assert.Equal(37, CountSide(inventory, "debugging"));

        foreach (var row in inventory)
        {
            Assert.Equal("yes", RequiredString(row, "alreadyInRuntime"));
            Assert.Equal("keep-existing", RequiredString(row, "action"));
            Assert.Equal(ManualId, RequiredString(row, "sourceManualId"));
            Assert.Equal("artifacts/manual-intake/sources/gree/Service Manual for GMV6 v_2020.09.pdf", RequiredString(row, "sourceManualPath"));
            Assert.Contains(RequiredString(row, "existingRuntimePath"), runtimeFiles);
            Assert.StartsWith("gmv6/", RequiredString(row, "proposedRuntimeFolder"), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(row, "sourceMeaningEn")));
        }

        var addPlan = ReadCsv(Path.Combine(ReportDirectory, "gmv6-runtime-add-plan-11.csv"));
        Assert.Empty(addPlan);
    }

    [Fact]
    public void Gmv6PackageAndManualCountsMatchRuntimeJsonCounts()
    {
        AssertPackageCount("gree-gmv6-outdoor-fault-protection-codes.json", "outdoor", 121);
        AssertPackageCount("gree-gmv6-indoor-fault-codes.json", "indoor", 60);
        AssertPackageCount("gree-gmv6-status-codes.json", "status", 44);
        AssertPackageCount("gree-gmv6-debugging-codes.json", "debugging", 38);

        var manuals = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "manual-library",
            "manuals.json"));
        var manual = RequiredArray(manuals, "manuals")
            .OfType<JsonObject>()
            .Single(item => RequiredString(item, "manualId") == ManualId);

        Assert.Equal(ManualName, RequiredString(manual, "documentTitle"));
        Assert.Equal("Imported", RequiredString(manual, "importStatus"));
        Assert.Equal("DiagnosticScopeImported", RequiredString(manual, "coverageStatus"));
        Assert.Equal(255, RequiredInt(manual, "entriesImported"));

        var totalRuntimeCount = Directory.GetFiles(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "gree"), "*.json", SearchOption.AllDirectories).Length;
        Assert.Equal(1293, totalRuntimeCount);
    }

    [Fact]
    public void Gmv6RuntimeEntriesRemainManualVerifiedHighConfidence()
    {
        foreach (var path in Directory.GetFiles(Gmv6RuntimeDirectory, "*.json", SearchOption.AllDirectories))
        {
            var entry = ReadObject(path);

            Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
            Assert.Equal("High", RequiredString(entry, "confidence"));
            Assert.Contains(
                RequiredString(entry, "sourceName"),
                new[] { ManualName, "GMV6 DC Inverter VRF Units Service Manual" });
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(entry, "sourceReference")));
        }
    }

    [Fact]
    public void Gmv6AndMiniN2UseSameNormalizedMeaningWithoutMergingCards()
    {
        var gmv6 = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "gree",
            "gmv6",
            "status",
            "n2.json"));
        var mini = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "gree",
            "gmv-mini",
            "status",
            "n2.json"));

        Assert.Equal("gree-gmv6-status-n2", RequiredString(gmv6, "id"));
        Assert.Equal("gree-gmv-mini-status-n2", RequiredString(mini, "id"));
        Assert.Equal("GMV6", RequiredString(gmv6, "series"));
        Assert.Equal("GMV Mini", RequiredString(mini, "series"));

        AssertN2Text(gmv6, "Gree GMV6 n2");
        AssertN2Text(mini, "Gree GMV Mini n2");
    }

    [Fact]
    public void ManualReviewReportKeepsNonGmv6TrackedCodesOutsideRuntime()
    {
        var review = ReadCsv(Path.Combine(ReportDirectory, "gmv6-manual-review-11.csv"));
        var codes = review.Select(row => row["code"]).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(17, review.Count);
        Assert.DoesNotContain("FH", codes);
        Assert.DoesNotContain("N2", codes);
        Assert.DoesNotContain("Ho", codes);

        foreach (var code in new[] { "E5", "eH", "F2", "F4", "Ld" })
        {
            var row = Assert.Single(review, item => item["code"] == code);
            Assert.Equal("NeedGmvWRuntimeStructure", row["trackingStatus"]);
        }

        foreach (var code in new[] { "Eb", "Fy", "JJ", "Jn", "Jy" })
        {
            var row = Assert.Single(review, item => item["code"] == code);
            Assert.Equal("BlockedNoManualEvidence", row["trackingStatus"]);
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
        var actualCount = Directory.GetFiles(Path.Combine(Gmv6RuntimeDirectory, folder), "*.json").Length;

        Assert.Equal(expectedCount, actualCount);
        Assert.Equal(expectedCount, RequiredInt(package, "entryCountExpected"));
    }

    private static void AssertN2Text(JsonObject entry, string expectedTitlePrefix)
    {
        foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
        {
            Assert.StartsWith(expectedTitlePrefix, RequiredString(text, "title"), StringComparison.Ordinal);
            Assert.Contains(N2Meaning, RequiredString(text, "title"), StringComparison.Ordinal);
            Assert.Contains(N2Meaning, RequiredString(text, "summary"), StringComparison.Ordinal);
        }
    }

    private static int CountSide(IReadOnlyCollection<JsonObject> inventory, string side) =>
        inventory.Count(row => RequiredString(row, "equipmentSide") == side);

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
