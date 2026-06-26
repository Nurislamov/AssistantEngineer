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

        Assert.Equal(262, entries.Count);
        foreach (var code in ExpectedBlockedCodes)
        {
            Assert.DoesNotContain(
                entries,
                entry => entry.Manufacturer == "Gree" &&
                    entry.Series == "GMV6" &&
                    string.Equals(entry.Code, code, StringComparison.Ordinal));
        }
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

        Assert.Equal(262, entries.Count);
        Assert.Equal(0, reviews.Count(item => RequiredString(item, "decision") == "added-runtime"));
        foreach (var item in reviews)
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
}
