using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvRemainingRuntimeCardsTests
{
    private static readonly string PreviewPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "remaining-runtime-candidates",
        "remaining-runtime-candidates-preview.json");

    private static readonly string CandidateJsonPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "remaining-runtime-candidates",
        "candidate-runtime-json.json");

    private static readonly string ManualReviewBatch9JsonPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "manual-review-batch-9",
        "manual-review-9.json");

    private static readonly string ManualReviewBatch9CsvPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "manual-review-batch-9",
        "manual-review-9.csv");

    private static readonly string CodeStatusRegistryJsonPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "code-status-registry",
        "gree-code-status-registry.json");

    private static readonly string CodeStatusRegistryCsvPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "code-status-registry",
        "gree-code-status-registry.csv");

    private static readonly string CodeStatusRegistryReadmePath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "code-status-registry",
        "README.md");

    private static readonly string[] ExpectedBlockedCodes =
    [
        "by",
        "E5",
        "E6",
        "E7",
        "E9",
        "eA",
        "Eb",
        "EE",
        "eH",
        "F2",
        "F4",
        "FH",
        "Fy",
        "Ho",
        "JJ",
        "Jn",
        "Jy",
        "Ld",
        "N2",
        "No"
    ];

    private static readonly string[] ExpectedPromotedCodes =
    [
        "FH",
        "N2"
    ];

    private static readonly string[] ExpectedStillBlockedCodes =
        ExpectedBlockedCodes
            .Except(ExpectedPromotedCodes, StringComparer.Ordinal)
            .ToArray();

    private static readonly IReadOnlyDictionary<string, string> ExpectedRegistryStatuses =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["by"] = "BlockedNoiseOrVisualAmbiguity",
            ["E5"] = "NeedGmvWRuntimeStructure",
            ["E6"] = "ManualReviewNoMiniRuntime",
            ["E7"] = "NeedSeriesDecision",
            ["E9"] = "NeedSeriesDecision",
            ["eA"] = "NeedSeriesDecision",
            ["Eb"] = "BlockedNoManualEvidence",
            ["EE"] = "BlockedNoiseOrSeriesMismatch",
            ["eH"] = "NeedGmvWRuntimeStructure",
            ["F2"] = "NeedGmvWRuntimeStructure",
            ["F4"] = "NeedGmvWRuntimeStructure",
            ["FH"] = "RuntimeAdded",
            ["Fy"] = "BlockedNoManualEvidence",
            ["Ho"] = "AliasToExistingCode",
            ["JJ"] = "BlockedNoManualEvidence",
            ["Jn"] = "BlockedNoManualEvidence",
            ["Jy"] = "BlockedNoManualEvidence",
            ["Ld"] = "NeedGmvWRuntimeStructure",
            ["N2"] = "RuntimeAdded",
            ["No"] = "BlockedNoiseOrVisualAmbiguity"
        };

    [Fact]
    public void RemainingRuntimeCandidatePreviewDocumentsBlockedCards()
    {
        var preview = ReadJsonObject(PreviewPath);

        Assert.Equal(1, RequiredInt32(preview, "schemaVersion"));
        Assert.Equal("ED-24GEC.8", RequiredString(preview, "stage"));
        Assert.Equal("remaining-runtime-candidates-preview", RequiredString(preview, "status"));
        Assert.False(RequiredBoolean(preview, "runtimeEnabled"));
        Assert.False(RequiredBoolean(preview, "diagnosticsRuntimeEnabled"));

        Assert.Empty(RequiredArray(preview, "candidates"));
        Assert.Empty(ReadJsonArray(CandidateJsonPath));

        var blocked = RequiredArray(preview, "blockedManualReview")
            .Select(Assert.IsType<JsonObject>)
            .OrderBy(item => RequiredString(item, "code"), StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ExpectedBlockedCodes.Order(StringComparer.Ordinal).ToArray(),
            blocked.Select(item => RequiredString(item, "code")).ToArray());

        foreach (var item in blocked)
        {
            Assert.Equal("blocked-manual-review", RequiredString(item, "decision"));
            Assert.Equal("review-template", RequiredString(item, "reviewStatus"));
            Assert.Equal("not-started", RequiredString(item, "extractionStatus"));
            Assert.False(RequiredBoolean(item, "approved"));
            Assert.False(RequiredBoolean(item, "readyForBotKnowledge"));
            Assert.False(RequiredBoolean(item, "alreadyRuntime"));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "sourcePath")));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "rawCardPath")));
            Assert.StartsWith(
                "data/equipment-diagnostics/error-knowledge/gree/gmv6/",
                RequiredString(item, "proposedRuntimePath"),
                StringComparison.Ordinal);

            var reason = RequiredString(item, "blockReason");
            Assert.Contains("review-template-not-extracted", reason, StringComparison.Ordinal);
            Assert.Contains("missing-normalized-runtime-text", reason, StringComparison.Ordinal);
            Assert.Contains("not-approved", reason, StringComparison.Ordinal);
            Assert.Contains("not-ready-for-bot-knowledge", reason, StringComparison.Ordinal);
        }

        Assert.Contains(
            blocked,
            item => RequiredString(item, "code") == "Ho" &&
                RequiredString(item, "blockReason").Contains("visual-ambiguity", StringComparison.Ordinal));
        Assert.Contains(
            blocked,
            item => RequiredString(item, "code") == "No" &&
                RequiredString(item, "blockReason").Contains("visual-ambiguity", StringComparison.Ordinal));
        Assert.Contains(
            blocked,
            item => RequiredString(item, "code") == "N2" &&
                RequiredString(item, "blockReason").Contains("gmv-mini-code-conflict", StringComparison.Ordinal));
    }

    [Fact]
    public void BlockedRemainingCardsAreNotLoadedIntoGmv6Runtime()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();

        var entries = source.GetEntries();

        Assert.Equal(391, entries.Count);
        foreach (var code in ExpectedStillBlockedCodes)
        {
            Assert.DoesNotContain(
                entries,
                entry => entry.Manufacturer == "Gree" &&
                    entry.Series == "GMV6" &&
                    string.Equals(entry.Code, code, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ManualConfirmedGmv6FhAndN2AreLoadedWithoutHoOrEeRuntimeCards()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();
        var entries = source.GetEntries();

        var fh = Assert.Single(entries, entry => entry.Id == "gree-gmv6-outdoor-fh");
        Assert.Equal("Gree", fh.Manufacturer);
        Assert.Equal("GMV6", fh.Series);
        Assert.Equal("FH", fh.Code);
        Assert.Equal("Abnormal Current Sensor of Compressor 1", fh.SourceMeaning);
        Assert.Equal("ManualVerified", fh.VerificationStatus);
        Assert.Equal("High", fh.Confidence);
        Assert.Contains(fh.SourceReferences, reference =>
            reference.ManualId == "gree-gmv6-service-manual-2020-09" &&
            reference.VerificationStatus == "ManualVerified" &&
            reference.Confidence == "High");

        var gmv6N2 = Assert.Single(entries, entry => entry.Id == "gree-gmv6-status-n2");
        Assert.Equal("GMV6", gmv6N2.Series);
        Assert.Equal("n2", gmv6N2.Code);
        Assert.Equal("Status", gmv6N2.SignalType.ToString());
        Assert.Contains("Maximum Capacity Configuration", gmv6N2.SourceMeaning, StringComparison.Ordinal);
        Assert.Contains(gmv6N2.SourceReferences, reference =>
            reference.ManualId == "gree-gmv6-service-manual-2020-09" &&
            reference.VerificationStatus == "ManualVerified" &&
            reference.Confidence == "High");

        Assert.Single(entries, entry => entry.Id == "gree-gmv-mini-status-n2");
        Assert.DoesNotContain(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.Series == "GMV6" &&
            string.Equals(entry.Code, "Ho", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.Series == "GMV6" &&
            string.Equals(entry.Code, "EE", StringComparison.Ordinal));

        Assert.False(File.Exists(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "gree",
            "gmv6",
            "outdoor",
            "ho.json")));
    }

    [Fact]
    public void ManualConfirmedGmv6RuntimeTextsAvoidInternalWordsAndUnsafeConsumerAdvice()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();
        var entries = source.GetEntries()
            .Where(entry => entry.Id is "gree-gmv6-outdoor-fh" or "gree-gmv6-status-n2")
            .ToArray();

        Assert.Equal(2, entries.Length);
        foreach (var entry in entries)
        {
            foreach (var text in entry.Texts)
            {
                var visibleText = string.Join(
                    " ",
                    text.Title,
                    text.Summary,
                    text.SafetyNote,
                    string.Join(" ", text.PossibleCauses),
                    string.Join(" ", text.CheckSteps),
                    string.Join(" ", text.DoNotAdvise),
                    text.RecommendedAction,
                    text.SourceNote);

                AssertNoInternalCatalogWords(visibleText);
            }

            var consumer = Assert.Single(entry.Texts, text => text.Audience.ToString() == "Consumer");
            AssertNoUnsafeConsumerAdvice(string.Join(
                " ",
                consumer.SafetyNote,
                string.Join(" ", consumer.CheckSteps),
                string.Join(" ", consumer.RecommendedAction)));
        }
    }

    [Fact]
    public void CodeStatusRegistryReflectsManualConfirmedRuntimeBatch()
    {
        var registry = ReadJsonArray(CodeStatusRegistryJsonPath)
            .Select(Assert.IsType<JsonObject>)
            .ToDictionary(item => RequiredString(item, "Code"), StringComparer.Ordinal);

        Assert.Equal("RuntimeAdded", RequiredString(registry["FH"], "TrackingStatus"));
        Assert.Equal(
            "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/fh.json",
            RequiredString(registry["FH"], "ExistingRuntimePaths"));

        Assert.Equal("RuntimeAdded", RequiredString(registry["N2"], "TrackingStatus"));
        Assert.Contains(
            "data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/n2.json",
            RequiredString(registry["N2"], "ExistingRuntimePaths"),
            StringComparison.Ordinal);
        Assert.Contains(
            "data/equipment-diagnostics/error-knowledge/gree/gmv6/status/n2.json",
            RequiredString(registry["N2"], "ExistingRuntimePaths"),
            StringComparison.Ordinal);

        Assert.Equal("AliasToExistingCode", RequiredString(registry["Ho"], "TrackingStatus"));
        Assert.Equal(
            "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/h0.json",
            RequiredString(registry["Ho"], "ExistingRuntimePaths"));

        Assert.Equal("BlockedNoiseOrSeriesMismatch", RequiredString(registry["EE"], "TrackingStatus"));
        Assert.Equal("no", RequiredString(registry["EE"], "ExistingRuntime"));
    }

    [Fact]
    public void CodeStatusRegistryCsvJsonAndReadmeStayConsistentForTrackedCodes()
    {
        var json = ReadJsonArray(CodeStatusRegistryJsonPath)
            .Select(Assert.IsType<JsonObject>)
            .ToDictionary(item => RequiredString(item, "Code"), StringComparer.Ordinal);
        var csv = ReadCsv(CodeStatusRegistryCsvPath);
        var readme = File.ReadAllText(CodeStatusRegistryReadmePath);

        Assert.Equal(
            ExpectedBlockedCodes.Order(StringComparer.Ordinal).ToArray(),
            json.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(
            ExpectedBlockedCodes.Order(StringComparer.Ordinal).ToArray(),
            csv.Keys.Order(StringComparer.Ordinal).ToArray());

        foreach (var code in ExpectedBlockedCodes)
        {
            var jsonRow = json[code];
            var csvRow = csv[code];

            Assert.Equal(ExpectedRegistryStatuses[code], RequiredString(jsonRow, "TrackingStatus"));
            Assert.Equal(RequiredString(jsonRow, "TrackingStatus"), csvRow["TrackingStatus"]);
            Assert.Equal(RequiredString(jsonRow, "ExistingRuntime"), csvRow["ExistingRuntime"]);
            Assert.Equal(OptionalString(jsonRow, "ExistingRuntimePaths"), csvRow["ExistingRuntimePaths"]);
            Assert.Equal(OptionalString(jsonRow, "RuntimeTarget"), csvRow["RuntimeTarget"]);
        }

        AssertRegistryRuntime(json, "eH", "NeedGmvWRuntimeStructure", "no", "", "GMV-W / Versati");
        AssertRegistryRuntime(json, "Fy", "BlockedNoManualEvidence", "no", "", "");
        AssertRegistryRuntime(json, "Ho", "AliasToExistingCode", "alias", "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/h0.json", "GMV6 H0");
        AssertRegistryRuntime(json, "FH", "RuntimeAdded", "yes", "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/fh.json", "GMV6 outdoor FH");
        AssertRegistryRuntime(json, "N2", "RuntimeAdded", "yes", "data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/n2.json; data/equipment-diagnostics/error-knowledge/gree/gmv6/status/n2.json", "GMV6 status n2");
        AssertRegistryRuntime(json, "EE", "BlockedNoiseOrSeriesMismatch", "no", "", "GMV6");

        foreach (var code in new[] { "E5", "eH", "F2", "F4", "Ld" })
        {
            Assert.Equal("NeedGmvWRuntimeStructure", RequiredString(json[code], "TrackingStatus"));
        }

        Assert.Equal("ManualReviewNoMiniRuntime", RequiredString(json["E6"], "TrackingStatus"));
        Assert.Equal("BlockedNoiseOrVisualAmbiguity", RequiredString(json["by"], "TrackingStatus"));
        Assert.Equal("BlockedNoiseOrVisualAmbiguity", RequiredString(json["No"], "TrackingStatus"));
        foreach (var code in new[] { "Eb", "Fy", "JJ", "Jn", "Jy" })
        {
            Assert.Equal("BlockedNoManualEvidence", RequiredString(json[code], "TrackingStatus"));
        }

        foreach (var code in new[] { "FH", "N2", "Ho", "EE", "eH", "Fy" })
        {
            var status = RequiredString(json[code], "TrackingStatus");
            Assert.Contains($"| {code} | {status} |", readme, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("| eH | RuntimeAdded |", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("eH | RuntimeAdded", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("Fy | AliasToExistingCode", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualReviewBatch9DocumentsAllBlockedCardsWithoutRuntimePromotion()
    {
        var report = ReadJsonObject(ManualReviewBatch9JsonPath);

        Assert.True(File.Exists(ManualReviewBatch9CsvPath));
        Assert.Equal(1, RequiredInt32(report, "schemaVersion"));
        Assert.Equal("ED-24GEC.9", RequiredString(report, "stage"));
        Assert.Equal("manual-review-complete", RequiredString(report, "status"));
        Assert.Equal(0, RequiredInt32(report, "runtimeEntriesAdded"));
        Assert.Equal(262, RequiredInt32(report, "totalRuntimeKnowledgeCount"));
        Assert.Equal(253, RequiredInt32(report, "gmv6RuntimeCount"));
        Assert.Empty(RequiredArray(report, "packageCountChanges"));

        var reviews = Batch9Reviews(report);
        Assert.Equal(
            ExpectedBlockedCodes.Order(StringComparer.Ordinal).ToArray(),
            reviews.Select(item => RequiredString(item, "code")).Order(StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(reviews, item => RequiredString(item, "decision") == "added-runtime");

        foreach (var item in reviews)
        {
            Assert.Contains(
                RequiredString(item, "decision"),
                new[] { "still-blocked", "needs-human-source-review", "reference-only" });
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "officialCode")));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "sourceCardPath")));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "rawCardPath")));
            Assert.Equal(string.Empty, OptionalString(item, "existingRuntimePath"));
            Assert.StartsWith(
                "data/equipment-diagnostics/error-knowledge/gree/gmv6/",
                RequiredString(item, "proposedRuntimePath"),
                StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "reason")));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "sourceMeaning")));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "equipmentType")));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(item, "categoryFolder")));
            Assert.NotEmpty(RequiredArray(item, "conflicts"));
            Assert.NotEmpty(RequiredArray(item, "safetyNotes"));
            Assert.NotEmpty(RequiredArray(item, "sourceReferences"));
            Assert.Equal(string.Empty, OptionalString(item, "addedRuntimeEntryId"));
        }
    }

    [Fact]
    public void ManualReviewBatch9StillBlockedCodesAreNotLoadedAsRuntimeEntries()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();
        var entries = source.GetEntries();
        var report = ReadJsonObject(ManualReviewBatch9JsonPath);
        var reviews = Batch9Reviews(report);

        Assert.Equal(391, entries.Count);
        Assert.Equal(0, reviews.Count(item => RequiredString(item, "decision") == "added-runtime"));
        foreach (var item in reviews.Where(item => !ExpectedPromotedCodes.Contains(RequiredString(item, "code"), StringComparer.Ordinal)))
        {
            var code = RequiredString(item, "code");
            Assert.DoesNotContain(
                entries,
                entry => entry.Manufacturer == "Gree" &&
                    entry.Series == "GMV6" &&
                    string.Equals(entry.Code, code, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ManualReviewBatch9DocumentsVisualAndMiniConflictGuards()
    {
        var reviews = Batch9Reviews(ReadJsonObject(ManualReviewBatch9JsonPath));

        AssertReviewConflict(reviews, "Ho", "visual ambiguity");
        AssertReviewConflict(reviews, "No", "visual ambiguity");
        AssertReviewConflict(reviews, "N2", "GMV Mini conflict");
        AssertReviewConflict(reviews, "by", "lowercase by visual ambiguity");
        AssertReviewConflict(reviews, "eA", "official image shows EA");
        AssertReviewConflict(reviews, "eH", "official image shows EH");
    }

    [Fact]
    public void RemainingSourceToRuntimeMappingCoversAllOfficialSupportReviewCards()
    {
        var preview = ReadJsonObject(PreviewPath);
        var mapping = RequiredArray(preview, "sourceToRuntimeMapping")
            .Select(Assert.IsType<JsonObject>)
            .ToArray();

        Assert.Equal(256, mapping.Length);
        Assert.Equal(236, mapping.Count(item => RequiredString(item, "decision") == "already-runtime"));
        Assert.Equal(20, mapping.Count(item => RequiredString(item, "decision") == "blocked-manual-review"));
        Assert.Equal(0, mapping.Count(item => RequiredString(item, "decision") == "safe-to-generate"));
    }

    private static JsonObject[] Batch9Reviews(JsonObject report) =>
        RequiredArray(report, "reviews")
            .Select(Assert.IsType<JsonObject>)
            .ToArray();

    private static void AssertReviewConflict(JsonObject[] reviews, string code, string expected)
    {
        var item = Assert.Single(reviews, review => RequiredString(review, "code") == code);
        var conflicts = RequiredArray(item, "conflicts")
            .Select(node => node!.GetValue<string>())
            .ToArray();

        Assert.Contains(conflicts, conflict => conflict.Contains(expected, StringComparison.OrdinalIgnoreCase));
        Assert.NotEqual("added-runtime", RequiredString(item, "decision"));
    }

    private static JsonObject ReadJsonObject(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);
        return Assert.IsType<JsonObject>(node);
    }

    private static JsonArray ReadJsonArray(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);
        return Assert.IsType<JsonArray>(node);
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ReadCsv(string path)
    {
        Assert.True(File.Exists(path), $"CSV file does not exist: {path}");
        var lines = File.ReadAllLines(path);
        Assert.NotEmpty(lines);

        var headers = ParseCsvLine(lines[0]);
        var codeIndex = Array.IndexOf(headers, "Code");
        Assert.True(codeIndex >= 0, "CSV must contain Code column.");

        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseCsvLine)
            .Select(values =>
            {
                Assert.Equal(headers.Length, values.Length);
                return (Code: values[codeIndex], Row: (IReadOnlyDictionary<string, string>)headers
                    .Select((header, index) => (header, value: values[index]))
                    .ToDictionary(item => item.header, item => item.value, StringComparer.Ordinal));
            })
            .ToDictionary(item => item.Code, item => item.Row, StringComparer.Ordinal);
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

    private static string OptionalString(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return node.GetValue<string>();
    }

    private static int RequiredInt32(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return node.GetValue<int>();
    }

    private static bool RequiredBoolean(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return node.GetValue<bool>();
    }

    private static void AssertRegistryRuntime(
        IReadOnlyDictionary<string, JsonObject> registry,
        string code,
        string status,
        string existingRuntime,
        string existingRuntimePaths,
        string runtimeTarget)
    {
        var row = registry[code];
        Assert.Equal(status, RequiredString(row, "TrackingStatus"));
        Assert.Equal(existingRuntime, RequiredString(row, "ExistingRuntime"));
        Assert.Equal(existingRuntimePaths, OptionalString(row, "ExistingRuntimePaths"));
        Assert.Equal(runtimeTarget, OptionalString(row, "RuntimeTarget"));
    }

    private static void AssertNoInternalCatalogWords(string text)
    {
        foreach (var forbidden in new[]
        {
            "support-каталог",
            "reference-only",
            "справочная запись",
            "raw",
            "review",
            "staging",
            "runtime",
            "internal",
            "sourceMeaning",
            "machine translated"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertNoUnsafeConsumerAdvice(string text)
    {
        foreach (var forbidden in new[]
        {
            "откройте панель",
            "откройте панели",
            "разберите",
            "снимите крышку",
            "измерьте напряжение",
            "измерить напряжение",
            "замкните",
            "обойдите защит",
            "принудительный пуск"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
