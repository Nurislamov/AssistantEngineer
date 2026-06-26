using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvRuntimeOverlayStagingTests
{
    private static readonly string OverlayPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "runtime-overlay",
        "approved-runtime-overlay-preview.json");

    private static readonly IReadOnlyDictionary<string, string> ExpectedRuntimeTargets =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["A0"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/status/a0.json",
            ["C0"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/c0.json",
            ["C7"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/c7.json",
            ["E0"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/e0.json",
            ["E1"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/e1.json",
            ["F3"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/f3.json",
            ["H5"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/h5.json",
            ["L1"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/indoor/l1.json",
            ["o1"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/indoor/o1.json",
            ["P0"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/p0.json",
            ["P1"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/p1.json",
            ["P2"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/p2.json",
            ["U0"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/u0.json",
            ["U2"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/u2.json",
            ["U3"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/u3.json",
            ["U4"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/u4.json",
            ["U5"] = "data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/u5.json",
        };

    [Fact]
    public void RuntimeOverlayStagingMapsOnlyApprovedGmvPriorityTargets()
    {
        var overlay = ReadJsonObject(OverlayPath);

        Assert.Equal(1, RequiredInt32(overlay, "schemaVersion"));
        Assert.Equal("staging-runtime-overlay-preview", RequiredString(overlay, "status"));
        Assert.Equal("ED-24GEC.5D", RequiredString(overlay, "stage"));
        Assert.Equal("Gree", RequiredString(overlay, "brand"));
        Assert.Equal("GMV", RequiredString(overlay, "model"));
        Assert.Equal("GMV", RequiredString(overlay, "family"));
        Assert.False(RequiredBoolean(overlay, "runtimeEnabled"));
        Assert.False(RequiredBoolean(overlay, "diagnosticsRuntimeEnabled"));

        var entries = RequiredArray(overlay, "entries");
        Assert.Equal(ExpectedRuntimeTargets.Count, entries.Count);

        var seenCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entryNode in entries)
        {
            var entry = Assert.IsType<JsonObject>(entryNode);
            var code = RequiredString(entry, "code");

            Assert.True(seenCodes.Add(code), $"Duplicate overlay entry for code '{code}'.");
            Assert.True(ExpectedRuntimeTargets.TryGetValue(code, out var expectedRuntimePath), $"Unexpected overlay code '{code}'.");

            Assert.Equal("staging-overlay-preview", RequiredString(entry, "status"));
            Assert.False(RequiredBoolean(entry, "runtimeEnabled"));
            Assert.False(RequiredBoolean(entry, "diagnosticsRuntimeEnabled"));

            var runtimePath = NormalizePath(RequiredString(entry, "runtimePath"));
            var approvedPath = NormalizePath(RequiredString(entry, "approvedPath"));

            Assert.Equal(expectedRuntimePath, runtimePath);
            Assert.DoesNotContain("gmv-mini", runtimePath, StringComparison.OrdinalIgnoreCase);
            AssertRepoFileExists(runtimePath);
            AssertRepoFileExists(approvedPath);

            if (code == "C0")
            {
                Assert.Equal("data/equipment-diagnostics/error-knowledge/gree/gmv6/debugging/c0.json", runtimePath);
            }

            var proposedOverlay = RequiredObject(entry, "overlay");

            Assert.Equal("gree-official-support-error-catalog", RequiredString(proposedOverlay, "referenceSource"));
            Assert.Equal("ED-24GEC.4B", RequiredString(proposedOverlay, "referenceStage"));
            Assert.Equal(approvedPath, NormalizePath(RequiredString(proposedOverlay, "referenceApprovedPath")));
            AssertRepoFileExists(NormalizePath(RequiredString(proposedOverlay, "primaryRawCardPath")));
            Assert.False(RequiredBoolean(proposedOverlay, "runtimeEnabled"));
            Assert.False(RequiredBoolean(proposedOverlay, "diagnosticsRuntimeEnabled"));

            Assert.NotEmpty(RequiredString(proposedOverlay, "titleRu"));
            Assert.NotEmpty(RequiredString(proposedOverlay, "meaningRu"));
            Assert.NotEmpty(RequiredString(proposedOverlay, "severityRu"));
            Assert.NotEmpty(RequiredString(proposedOverlay, "userSafeAnswerRu"));
            Assert.NotEmpty(RequiredString(proposedOverlay, "technicianAnswerRu"));
            Assert.NotEmpty(RequiredArray(proposedOverlay, "possibleCausesRu"));
            Assert.NotEmpty(RequiredArray(proposedOverlay, "checksRu"));
            Assert.NotEmpty(RequiredArray(proposedOverlay, "serviceNotesRu"));
        }

        Assert.Equal(
            ExpectedRuntimeTargets.Keys.Order(StringComparer.Ordinal).ToArray(),
            seenCodes.Order(StringComparer.Ordinal).ToArray());

        var blockedMappings = RequiredArray(overlay, "blockedMappings");
        var blocked = Assert.IsType<JsonObject>(Assert.Single(blockedMappings));

        Assert.Equal("C0", RequiredString(blocked, "code"));
        Assert.Equal("blocked", RequiredString(blocked, "status"));
        Assert.Equal(
            "data/equipment-diagnostics/error-knowledge/gree/gmv-mini/indoor/c0.json",
            NormalizePath(RequiredString(blocked, "runtimePath")));
        AssertRepoFileExists(NormalizePath(RequiredString(blocked, "runtimePath")));
        Assert.Contains("GMV Mini C0", RequiredString(blocked, "reason"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuntimeKnowledgeLoaderIgnoresReferenceOverlayStaging()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();

        var entries = source.GetEntries();

        Assert.Equal(264, entries.Count);
        Assert.DoesNotContain(entries, entry => entry.Id.Contains("runtime-overlay", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, entry => entry.PackageId.Contains("gree-official-support-error-catalog", StringComparison.OrdinalIgnoreCase));
    }

    private static JsonObject ReadJsonObject(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");

        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);

        return Assert.IsType<JsonObject>(node);
    }

    private static JsonObject RequiredObject(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];

        Assert.NotNull(node);

        return Assert.IsType<JsonObject>(node);
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

    private static void AssertRepoFileExists(string relativePath)
    {
        var fullPath = Path.Combine(
            TestPaths.RepoRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(File.Exists(fullPath), $"Expected repository file does not exist: {relativePath}");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
