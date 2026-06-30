using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXImport14Tests
{
    private const string ManualId = "gree-gmv-x-service-manual-2022-09";
    private const string DocumentCode = "GC202209-I";

    private static readonly string ReportDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "gmv-x-import-14");

    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly IReadOnlyDictionary<string, int> CategoryCounts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["indoor"] = 60,
            ["outdoor"] = 121,
            ["debugging"] = 38,
            ["status"] = 44
        };

    [Fact]
    public void ImportReportArtifactsExist()
    {
        foreach (var fileName in new[]
        {
            "README.md",
            "gmv-x-manual-inventory-14.csv",
            "gmv-x-manual-inventory-14.json",
            "imported-codes-14.csv",
            "imported-codes-14.json",
            "skipped-codes-14.csv",
            "manual-review-14.csv",
            "runtime-overlap-14.csv",
            "runtime-package-summary-14.csv"
        })
        {
            Assert.True(File.Exists(Path.Combine(ReportDirectory, fileName)), $"Missing report artifact: {fileName}");
        }
    }

    [Fact]
    public void ManualInventoryReferencesGc202209IAndImportsAllReadyRows()
    {
        var inventory = ReadArray(Path.Combine(ReportDirectory, "gmv-x-manual-inventory-14.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();
        var imported = ReadArray(Path.Combine(ReportDirectory, "imported-codes-14.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();

        Assert.Equal(263, inventory.Length);
        Assert.Equal(263, imported.Length);
        Assert.Equal(263, inventory.Count(row => RequiredString(row, "actionRecommendation") == "ready-for-import"));
        Assert.All(inventory, row =>
        {
            Assert.Equal(ManualId, RequiredString(row, "sourceManualId"));
            Assert.Equal(DocumentCode, RequiredString(row, "sourceDocument"));
            Assert.Equal("GMV X", RequiredString(row, "seriesCandidate"));
        });

        foreach (var (category, expected) in CategoryCounts)
            Assert.Equal(expected, inventory.Count(row => RequiredString(row, "categoryCandidate") == category));
    }

    [Fact]
    public void RuntimeAndPackageCountsMatchGmvXImport()
    {
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(136, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-mini"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-x"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(260, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(1184, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        AssertPackageCount("gree-gmv-x-indoor-fault-codes.json", 60);
        AssertPackageCount("gree-gmv-x-outdoor-fault-protection-codes.json", 121);
        AssertPackageCount("gree-gmv-x-debugging-codes.json", 38);
        AssertPackageCount("gree-gmv-x-status-codes.json", 44);

        Assert.False(Directory.Exists(Path.Combine(GreeRuntimeDirectory, "9-series-flex")));
        Assert.False(Directory.Exists(Path.Combine(GreeRuntimeDirectory, "flex")));
    }

    [Theory]
    [InlineData("E1", "outdoor")]
    [InlineData("H5", "outdoor")]
    [InlineData("C0", "debugging")]
    [InlineData("A0", "status")]
    [InlineData("L1", "indoor")]
    public void ImportedCardsAreManualVerifiedAndSourceBound(string code, string category)
    {
        var entry = ReadObject(Path.Combine(GreeRuntimeDirectory, "gmv-x", category, $"{code.ToLowerInvariant()}.json"));

        Assert.Equal($"gree-gmv-x-{category}-{code.ToLowerInvariant()}", RequiredString(entry, "id"));
        Assert.Equal("GMV X", RequiredString(entry, "series"));
        Assert.Equal(code, RequiredString(entry, "code"));
        Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
        Assert.Equal("High", RequiredString(entry, "confidence"));
        Assert.Equal(ManualId, RequiredString(entry, "sourceReferences", 0, "manualId"));
        Assert.Equal(DocumentCode, RequiredString(entry, "sourceReferences", 0, "documentCode"));
    }

    [Theory]
    [InlineData("Gree GMV X E1", "Gree GMV X — E1")]
    [InlineData("Gree X H5", "Gree GMV X — H5")]
    [InlineData("Gree X series C0", "Gree GMV X — C0")]
    [InlineData("Gree X-series A0", "Gree GMV X — A0")]
    public async Task ExplicitGmvXQueriesResolveGmvXCards(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 HR", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV Mini", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitGmvXUnknownCodeDoesNotFallbackToGmv6OrMini()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree X FH"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.DoesNotContain("Gree GMV6 FH", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV Mini", response.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree H5", "Gree GMV6 — H5")]
    [InlineData("Gree U3", "Gree GMV6 — U3")]
    [InlineData("Gree o1", "Gree GMV6 — o1")]
    [InlineData("Gree GMV6 A9", "Gree GMV6 — A9")]
    [InlineData("Gree GMV Mini n2", "Gree GMV Mini — n2")]
    public async Task ExistingGmv6AndMiniPriorityQueriesRemainStable(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnqualifiedN2StillAsksForGmv6OrGmvMini()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("GMV6", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV Mini", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void VisibleTextAvoidsInternalWordsAndUnsafeConsumerAdvice()
    {
        var forbidden = new[]
        {
            "support-каталог",
            "reference-only",
            "sourceMeaning",
            "machine translated",
            "runtime",
            "staging",
            "pipeline",
            "измерьте напряжение",
            "измерьте ток",
            "откройте",
            "замкните",
            "замените плату",
            "замените датчик",
            "замените компрессор",
            "принудительный запуск"
        };

        foreach (var path in Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-x"), "*.json", SearchOption.AllDirectories))
        {
            var entry = ReadObject(path);
            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
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
                yield return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
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
