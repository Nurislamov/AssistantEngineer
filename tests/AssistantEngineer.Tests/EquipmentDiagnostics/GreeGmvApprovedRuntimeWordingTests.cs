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

    private static readonly string[] ForbiddenVisibleTerms =
    [
        "review-РїРѕР»",
        "review-РєР°СЂС‚",
        "raw card",
        "raw-РєР°СЂС‚",
        "approved",
        "runtime",
        "РґРёР°РіРЅРѕСЃС‚РёС‡РµСЃРєР°СЏ Р±Р°Р·Р°",
        "commissioning"
    ];

    [Fact]
    public void ApprovedPriorityGmvRuntimeTextsAreHumanReadableAndClean()
    {
        foreach (var (code, runtimePath) in RuntimeTargets)
        {
            var runtime = ReadObject(Path.Combine(
                TestPaths.RepoRoot,
                runtimePath.Replace('/', Path.DirectorySeparatorChar)));

            var texts = RequiredArray(runtime, "texts")
                .OfType<JsonObject>()
                .Where(IsRussianText)
                .ToArray();

            Assert.NotEmpty(texts);
            Assert.Contains(texts, text => IsAudience(text, "Consumer"));
            Assert.Contains(texts, text => !IsAudience(text, "Consumer"));

            foreach (var text in texts)
            {
                Assert.StartsWith($"Gree GMV {code}", RequiredString(text, "title"), StringComparison.OrdinalIgnoreCase);
                Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "summary")));
                Assert.NotEmpty(RequiredStringArray(text, "checkSteps"));
                Assert.NotEmpty(RequiredStringArray(text, "possibleCauses"));
                Assert.NotEmpty(RequiredStringArray(text, "doNotAdvise"));
                Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "safetyNote")));
                Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "recommendedAction")));

                var visibleBlob = text.ToJsonString();
                foreach (var forbidden in ForbiddenVisibleTerms)
                {
                    Assert.DoesNotContain(forbidden, visibleBlob, StringComparison.OrdinalIgnoreCase);
                }
            }
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

    private static bool IsRussianText(JsonObject text)
    {
        if (text.TryGetPropertyValue("locale", out var localeNode) && localeNode is not null)
        {
            return localeNode.GetValue<string>().StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        }

        if (text.TryGetPropertyValue("language", out var languageNode) && languageNode is not null)
        {
            return languageNode.GetValue<string>().StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        }

        return text.ContainsKey("title") && text.ContainsKey("summary");
    }

    private static bool IsAudience(JsonObject text, string expected)
    {
        if (!text.TryGetPropertyValue("audience", out var audienceNode) || audienceNode is null)
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

    private static string[] RequiredStringArray(JsonObject obj, string propertyName)
    {
        var array = RequiredArray(obj, propertyName);
        var values = array
            .Select(node => node?.GetValue<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        Assert.Equal(array.Count, values.Length);
        return values;
    }
}