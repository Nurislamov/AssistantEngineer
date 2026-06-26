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
                .Select(item => SanitizeVisibleRuntimeText(item!.GetValue<string>()))
                .ToArray();

            var texts = RequiredArray(runtime, "texts")
                .OfType<JsonObject>()
                .Where(IsRussianText)
                .ToArray();

            Assert.NotEmpty(texts);
            Assert.Contains(texts, text => RequiredString(text, "title") == expectedTitle);
            Assert.Contains(texts, text => ArrayEquals(RequiredArray(text, "checkSteps"), expectedChecks));

            var consumerSummary = SanitizeVisibleRuntimeText(RequiredString(normalizedRu, "userSafeAnswerRu"));
            var technicianSummary = SanitizeVisibleRuntimeText(RequiredString(normalizedRu, "technicianAnswerRu"));

            Assert.Contains(texts, text =>
                IsAudience(text, "Consumer") &&
                !string.IsNullOrWhiteSpace(RequiredString(text, "summary")));

            Assert.Contains(texts, text =>
                !IsAudience(text, "Consumer") &&
                !string.IsNullOrWhiteSpace(RequiredString(text, "summary")));

            foreach (var text in texts)
            {
                var visibleBlob = text.ToJsonString();

                Assert.DoesNotContain("review-пол", visibleBlob, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("review-карт", visibleBlob, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("raw card", visibleBlob, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("raw-карт", visibleBlob, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("approved", visibleBlob, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("runtime", visibleBlob, StringComparison.OrdinalIgnoreCase);
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

    private static string SanitizeVisibleRuntimeText(string value)
    {
        return value
            .Replace("согласно заполненным review-полям и raw card", "по проверенному описанию сервисной карты", StringComparison.Ordinal)
            .Replace("согласно заполненным review-полям", "по проверенному описанию", StringComparison.Ordinal)
            .Replace("заполненным review-полям", "проверенному описанию", StringComparison.Ordinal)
            .Replace("заполненные review-поля", "проверенное описание", StringComparison.Ordinal)
            .Replace("review-полям", "проверенному описанию", StringComparison.Ordinal)
            .Replace("review-поля", "проверенное описание", StringComparison.Ordinal)
            .Replace("по raw card", "по сервисной карте", StringComparison.Ordinal)
            .Replace("по raw-карте", "по сервисной карте", StringComparison.Ordinal)
            .Replace("raw-карточке", "сервисной карте", StringComparison.Ordinal)
            .Replace("raw-карта", "сервисная карта", StringComparison.Ordinal)
            .Replace("raw card", "сервисная карта", StringComparison.Ordinal)
            .Replace("Перед approved/runtime проверить", "Перед окончательным выводом проверить", StringComparison.Ordinal)
            .Replace("без approved-проверки", "без подтверждения по сервисной карте", StringComparison.Ordinal)
            .Replace("Не включать runtime", "Не использовать как окончательный вывод", StringComparison.Ordinal)
            .Replace("runtime", "диагностическая база", StringComparison.Ordinal)
            .Replace("approved", "подтверждённый", StringComparison.Ordinal);
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