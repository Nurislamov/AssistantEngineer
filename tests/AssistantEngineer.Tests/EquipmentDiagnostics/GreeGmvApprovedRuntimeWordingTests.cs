using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvApprovedRuntimeWordingTests
{
    private static readonly IReadOnlyDictionary<string, string> RuntimeTargets =
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
    public void ApprovedPriorityGmvWordingIsAppliedToRuntimeRussianTexts()
    {
        foreach (var (code, runtimePath) in RuntimeTargets)
        {
            var approved = ReadObject(Path.Combine(
                TestPaths.RepoRoot,
                "data",
                "reference",
                "gree-official-support-error-catalog",
                "approved",
                $"Gree-GMV-{code}.approved.json"));

            var runtime = ReadObject(Path.Combine(
                TestPaths.RepoRoot,
                runtimePath.Replace('/', Path.DirectorySeparatorChar)));

            var normalizedRu = RequiredObject(approved, "normalizedRu");
            var expectedTitle = RequiredString(normalizedRu, "titleRu");
            var expectedChecks = RequiredArray(normalizedRu, "checksRu")
                .Select(item => item!.GetValue<string>())
                .ToArray();

            var texts = RequiredArray(runtime, "texts")
                .OfType<JsonObject>()
                .Where(IsRussianText)
                .ToArray();

            Assert.NotEmpty(texts);
            Assert.Contains(texts, text => RequiredString(text, "title") == expectedTitle);
            Assert.Contains(texts, text => ArrayEquals(RequiredArray(text, "checkSteps"), expectedChecks));

            var consumerSummary = RequiredString(normalizedRu, "userSafeAnswerRu");
            var technicianSummary = RequiredString(normalizedRu, "technicianAnswerRu");

            Assert.Contains(texts, text =>
                IsAudience(text, "Consumer") &&
                RequiredString(text, "summary") == consumerSummary);

            Assert.Contains(texts, text =>
                !IsAudience(text, "Consumer") &&
                RequiredString(text, "summary") == technicianSummary);
        }
    }

    [Fact]
    public void ApprovedPriorityGmvWordingDoesNotChangeMiniC0RuntimeTarget()
    {
        var miniC0 = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "gree",
            "gmv-mini",
            "indoor",
            "c0.json"));

        var textBlob = miniC0.ToJsonString();

        Assert.DoesNotContain("gree-official-support-error-catalog/approved/Gree-GMV-C0.approved.json", textBlob, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree-GMV-C0", textBlob, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ArrayEquals(JsonArray actualArray, IReadOnlyList<string> expected)
    {
        var actual = actualArray
            .Select(item => item!.GetValue<string>())
            .ToArray();

        return actual.SequenceEqual(expected, StringComparer.Ordinal);
    }

    private static bool IsRussianText(JsonObject text)
    {
        if (text.TryGetPropertyValue("language", out var languageNode) &&
            languageNode is not null)
        {
            return languageNode.GetValue<string>().StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        }

        return text.ContainsKey("title") && text.ContainsKey("summary");
    }

    private static bool IsAudience(JsonObject text, string expected)
    {
        if (!text.TryGetPropertyValue("audience", out var audienceNode) ||
            audienceNode is null)
        {
            return false;
        }

        return string.Equals(audienceNode.GetValue<string>(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonObject ReadObject(string path)
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
}