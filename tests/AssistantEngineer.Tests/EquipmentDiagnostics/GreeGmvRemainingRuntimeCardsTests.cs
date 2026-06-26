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
