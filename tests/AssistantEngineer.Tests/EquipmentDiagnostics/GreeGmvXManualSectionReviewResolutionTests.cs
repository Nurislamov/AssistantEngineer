using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXManualSectionReviewResolutionTests
{
    private static readonly string GreeRoot = Path.Combine(TestPaths.RepoRoot, "data", "equipment-diagnostics", "error-knowledge", "gree");
    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");
    private static readonly string IndoorRoot = Path.Combine(GmvXRoot, "indoor");

    private static readonly string[] ReviewCodes = ["d5", "d8", "dE", "L2", "L6", "LH"];

    private static readonly string[] ForbiddenVisibleFragments =
    [
        "Подтвердите код",
        "Сверьте модель",
        "Дальнейшие действия",
        "Точная причина зависит",
        "manual",
        "source",
        "packageId",
        "руководство",
        "основание",
        "по таблице",
        "классифицирован по таблице",
        "к датчика"
    ];

    [Fact]
    public void AllReviewCodesExistWithCleanGmvXTitles()
    {
        var entries = ReadEntries();

        Assert.Equal(6, entries.Count);
        Assert.Equal(ReviewCodes.Order(StringComparer.Ordinal), entries.Keys.Order(StringComparer.Ordinal));

        foreach (var (code, entry) in entries)
        {
            var visible = VisibleBlob(entry);
            Assert.Contains($"Gree GMV X — {code} —", visible, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV6", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree GMV —", visible, StringComparison.OrdinalIgnoreCase);
            foreach (var forbidden in ForbiddenVisibleFragments)
                Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ReservedHeadingsStaySafeWithoutInventedDiagnosis()
    {
        AssertReserved("d5", "датчика температуры средней части трубки");
        AssertReserved("d8", "датчика температуры воды");
        AssertReserved("dE", "датчика CO2 внутреннего блока");
        AssertReserved("L2", "защите дополнительного электрического нагревателя");
        AssertReserved("LH", "предупреждению о низком качестве воздуха");
    }

    [Fact]
    public void L6UsesDocumentedNonFaultModeConflictBehavior()
    {
        var entry = ReadEntries()["L6"];
        var visible = VisibleBlob(entry);

        Assert.Contains("рабочее ограничение согласования режимов, а не отдельная поломка", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("через пять секунд", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("совместимый с текущим режимом наружного блока", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Охлаждение и осушение совместимы", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("вентиляция совместима с любым режимом", visible, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InventoryRunnerRecordsAllReviewDispositions()
    {
        var scriptPath = Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "equipment-diagnostics",
            "invoke-gmvx-manual-bound-closure-inventory.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("$manualSectionNeedsReviewCodes = @()", script, StringComparison.Ordinal);
        Assert.Contains("reviewDisposition", script, StringComparison.Ordinal);
        Assert.Contains("\"L6\" = [pscustomobject]@{", script, StringComparison.Ordinal);
        Assert.Contains("disposition = \"NonFaultSafe\"", script, StringComparison.Ordinal);
        Assert.Equal(5, CountOccurrences(script, "disposition = \"NotApplicableOrReserved\""));
    }

    [Fact]
    public void RuntimeCountsRemainStableAndGmv6IsUntouched()
    {
        Assert.Equal(1308, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(GmvXRoot, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(IndoorRoot, "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(GmvXRoot, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(GmvXRoot, "debugging"), "*.json").Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRoot, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
    }

    private static void AssertReserved(string code, string meaning)
    {
        var entry = ReadEntries()[code];
        var visible = VisibleBlob(entry);

        Assert.Contains("зарезервирован", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(meaning, visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("отдельная", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("диагностическая процедура", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(RequiredArray(entry, "texts")
            .OfType<JsonObject>()
            .SelectMany(text => RequiredArray(text, "possibleCauses")));
    }

    private static Dictionary<string, JsonObject> ReadEntries()
    {
        var codes = ReviewCodes.ToHashSet(StringComparer.Ordinal);
        return Directory.GetFiles(IndoorRoot, "*.json")
            .Select(path => Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path))))
            .Where(entry => codes.Contains(RequiredString(entry, "code")))
            .ToDictionary(entry => RequiredString(entry, "code"), StringComparer.Ordinal);
    }

    private static string VisibleBlob(JsonObject entry)
    {
        var parts = new List<string>();
        foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
        {
            foreach (var property in new[] { "title", "summary", "recommendedAction", "safetyNote", "sourceNote" })
                parts.Add(RequiredString(text, property));
            foreach (var property in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
                parts.AddRange(RequiredArray(text, property).OfType<JsonValue>().Select(value => value.GetValue<string>()));
        }
        return string.Join('\n', parts);
    }

    private static int CountOccurrences(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;

    private static string RequiredString(JsonObject entry, string propertyName)
    {
        Assert.True(entry.TryGetPropertyValue(propertyName, out var node), $"Missing property '{propertyName}'.");
        return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
    }

    private static JsonArray RequiredArray(JsonObject entry, string propertyName)
    {
        Assert.True(entry.TryGetPropertyValue(propertyName, out var node), $"Missing property '{propertyName}'.");
        return Assert.IsType<JsonArray>(node);
    }
}
