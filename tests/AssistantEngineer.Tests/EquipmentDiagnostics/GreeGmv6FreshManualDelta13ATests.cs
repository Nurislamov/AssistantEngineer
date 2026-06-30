using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmv6FreshManualDelta13ATests
{
    private const string FreshManualId = "gree-gmv6-service-manual-2022-03";
    private const string FreshDocumentCode = "GC202203-IV";

    private static readonly string ReportDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "gmv6-fresh-manual-delta-13A");

    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly IReadOnlyDictionary<string, string> DeltaCategories =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["A9"] = "status",
            ["n1"] = "status",
            ["qA"] = "status",
            ["qC"] = "status",
            ["qH"] = "status",
            ["qP"] = "status",
            ["qU"] = "status",
            ["Uy"] = "debugging"
        };

    [Fact]
    public void FreshManualDeltaReportArtifactsExist()
    {
        foreach (var fileName in new[]
        {
            "README.md",
            "gmv6-fresh-manual-inventory-13A.csv",
            "gmv6-fresh-manual-inventory-13A.json",
            "gmv6-runtime-delta-13A.csv",
            "gmv6-runtime-delta-13A.json",
            "imported-codes-13A.csv",
            "skipped-codes-13A.csv",
            "manual-review-13A.csv",
            "runtime-count-summary-13A.csv"
        })
        {
            Assert.True(File.Exists(Path.Combine(ReportDirectory, fileName)), $"Missing report artifact: {fileName}");
        }
    }

    [Fact]
    public void FreshManualInventoryCapturesGc202203Delta()
    {
        var inventory = ReadArray(Path.Combine(ReportDirectory, "gmv6-fresh-manual-inventory-13A.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();
        var delta = ReadArray(Path.Combine(ReportDirectory, "gmv6-runtime-delta-13A.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();

        Assert.Equal(263, inventory.Length);
        Assert.Equal(8, delta.Length);
        Assert.Equal(255, inventory.Count(row => RequiredString(row, "actionRecommendation") == "already-covered"));
        Assert.Equal(8, inventory.Count(row => RequiredString(row, "actionRecommendation") == "ready-for-import"));

        foreach (var code in DeltaCategories.Keys)
        {
            var row = Assert.Single(delta, item => RequiredString(item, "code") == code);
            Assert.Equal(FreshManualId, RequiredString(row, "sourceManualId"));
            Assert.Equal(FreshDocumentCode, RequiredString(row, "sourceDocument"));
            Assert.Equal("ready-for-import", RequiredString(row, "actionRecommendation"));
        }
    }

    [Fact]
    public void ImportedDeltaCodesAreManualVerifiedRuntimeCards()
    {
        foreach (var (code, category) in DeltaCategories)
        {
            var path = Path.Combine(GreeRuntimeDirectory, "gmv6", category, $"{code.ToLowerInvariant()}.json");
            var entry = ReadObject(path);

            Assert.Equal($"gree-gmv6-{category}-{code.ToLowerInvariant()}", RequiredString(entry, "id"));
            Assert.Equal(code, RequiredString(entry, "code"));
            Assert.Equal("GMV6", RequiredString(entry, "series"));
            Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
            Assert.Equal("High", RequiredString(entry, "confidence"));
            Assert.Equal(FreshDocumentCode, RequiredString(entry, "sourceReferences", 0, "documentCode"));
            Assert.Equal(FreshManualId, RequiredString(entry, "sourceReferences", 0, "manualId"));
            Assert.Equal($"gree-gmv6-{category}-codes", RequiredString(entry, "packageId"));
        }
    }

    [Fact]
    public void RuntimeAndPackageCountsMatchFreshDeltaImport()
    {
        Assert.Equal(60, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6", "indoor"), "*.json").Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6", "outdoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6", "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6", "debugging"), "*.json").Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(136, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-mini"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(1184, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        AssertPackageCount("gree-gmv6-status-codes.json", 44);
        AssertPackageCount("gree-gmv6-debugging-codes.json", 38);
    }

    [Fact]
    public void StageDoesNotChangeMiniOrAlternateFlexRuntimeScope()
    {
        foreach (var folder in new[] { "x-series", "9-series-flex", "flex" })
        {
            Assert.False(Directory.Exists(Path.Combine(GreeRuntimeDirectory, folder)), $"Unexpected runtime folder: {folder}");
        }

        Assert.Equal(136, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-mini"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-x"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(260, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories).Length);
    }

    [Theory]
    [InlineData("Gree GMV6 A9", "Gree GMV6 — A9")]
    [InlineData("Gree GMV6 n1", "Gree GMV6 — n1")]
    [InlineData("Gree GMV6 qA", "Gree GMV6 — qA")]
    [InlineData("Gree GMV6 Uy", "Gree GMV6 — Uy")]
    public async Task TelegramReturnsRepresentativeFreshDeltaCodes(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Set Back function", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sourceMeaning", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("runtime", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportedVisibleTextAvoidsUnsafeConsumerAdvice()
    {
        var forbidden = new[]
        {
            "измерьте напряжение",
            "измерьте ток",
            "откройте",
            "обойдите защит",
            "замкните",
            "замените плату",
            "замените датчик",
            "замените компрессор",
            "заправьте хладагент",
            "стравите хладагент",
            "принудительный запуск"
        };

        foreach (var (code, category) in DeltaCategories)
        {
            var entry = ReadObject(Path.Combine(GreeRuntimeDirectory, "gmv6", category, $"{code.ToLowerInvariant()}.json"));
            var texts = RequiredArray(entry, "texts").OfType<JsonObject>().ToArray();
            Assert.Equal(3, texts.Length);

            foreach (var text in texts)
            {
                foreach (var visible in VisibleValues(text))
                {
                    Assert.DoesNotContain("Set Back function", visible, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("machine translated", visible, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("sourceMeaning", visible, StringComparison.OrdinalIgnoreCase);
                }

                var consumer = RequiredString(text, "audience") == "Consumer";
                if (!consumer)
                    continue;

                var combined = string.Join(" ", VisibleValues(text));
                foreach (var fragment in forbidden)
                    Assert.DoesNotContain(fragment, combined, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static void AssertPackageCount(string packageFileName, int expected)
    {
        var package = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "packages",
            packageFileName));

        Assert.Equal(expected, RequiredInt(package, "entryCountExpected"));
    }

    private static IEnumerable<string> VisibleValues(JsonObject text)
    {
        yield return RequiredString(text, "title");
        yield return RequiredString(text, "summary");
        yield return RequiredString(text, "safetyNote");
        yield return RequiredString(text, "recommendedAction");
        yield return RequiredString(text, "sourceNote");

        foreach (var propertyName in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
        {
            foreach (var node in RequiredArray(text, propertyName))
            {
                yield return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
            }
        }
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            DefaultManufacturer = "Gree",
            MaxMessageLength = 900,
            AllowedChatIds = [7]
        });

        return services.BuildServiceProvider();
    }

    private static EquipmentDiagnosticTelegramUpdate Update(string text) =>
        new(UpdateId: 1, ChatId: 7, Username: "operator", Text: text, UserId: 11);

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
        Assert.False(string.IsNullOrWhiteSpace(value), $"Property {propertyName} must not be blank.");
        return value;
    }

    private static string RequiredString(JsonObject obj, string arrayName, int index, string propertyName)
    {
        var array = RequiredArray(obj, arrayName);
        var child = Assert.IsType<JsonObject>(array[index]);
        return RequiredString(child, propertyName);
    }

    private static int RequiredInt(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return node.GetValue<int>();
    }
}
