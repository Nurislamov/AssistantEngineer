using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmv6HrImport3Tests
{
    private const string ManualId = "gree-gmv6-hr-service-manual-2025-07";
    private const string OwnerManualId = "gree-gmv6-hr-owner-manual-2024-12";

    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    [Fact]
    public void RuntimeAndPackageCountsMatchGmv6HrImport()
    {
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(262, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6-hr"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(136, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-mini"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-x"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(260, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(1296, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        AssertPackageCount("gree-gmv6-hr-indoor-fault-codes.json", 60);
        AssertPackageCount("gree-gmv6-hr-outdoor-fault-protection-codes.json", 120);
        AssertPackageCount("gree-gmv6-hr-debugging-codes.json", 38);
        AssertPackageCount("gree-gmv6-hr-status-codes.json", 44);
    }

    [Theory]
    [InlineData("E0", "outdoor")]
    [InlineData("U4", "debugging")]
    [InlineData("C2", "debugging")]
    [InlineData("n2", "status")]
    [InlineData("A9", "status")]
    public void ImportedCardsAreManualVerifiedAndSourceBound(string code, string category)
    {
        var entry = ReadObject(Path.Combine(GreeRuntimeDirectory, "gmv6-hr", category, $"{code.ToLowerInvariant()}.json"));

        Assert.Equal($"gree-gmv6-hr-{category}-{code.ToLowerInvariant()}", RequiredString(entry, "id"));
        Assert.Equal("GMV6 HR", RequiredString(entry, "series"));
        Assert.Equal(code, RequiredString(entry, "code"));
        Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
        Assert.Equal("High", RequiredString(entry, "confidence"));
        Assert.Equal(ManualId, RequiredString(entry, "sourceReferences", 0, "manualId"));
        Assert.Equal("Not stated", RequiredString(entry, "sourceReferences", 0, "documentCode"));
    }

    [Theory]
    [InlineData("Gree GMV6 HR E0", "Gree GMV6 HR — E0")]
    [InlineData("Gree GMV6 HR U4", "Gree GMV6 HR — U4")]
    [InlineData("Gree GMV6 HR C2", "Gree GMV6 HR — C2")]
    [InlineData("Gree GMV6 HR n2", "Gree GMV6 HR — n2")]
    [InlineData("Gree GMV6 HR A9", "Gree GMV6 HR — A9")]
    public async Task ExplicitGmv6HrQueriesResolveGmv6HrCards(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Серия:</b> GMV6 HR", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 —", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV X", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV9 Flex", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlainGmv6QueryResolvesGmv6WithoutReturningHrCard()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV6 E0"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Gree GMV6 — E0", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Серия:</b> GMV6", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 HR — E0", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualRegistryRecordsHrServiceImportAndOwnerManualPending()
    {
        var registry = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "manual-library",
            "manuals.json"));
        var manuals = RequiredArray(registry, "manuals").OfType<JsonObject>().ToArray();
        var service = Assert.Single(manuals, item => RequiredString(item, "manualId") == ManualId);
        var owner = Assert.Single(manuals, item => RequiredString(item, "manualId") == OwnerManualId);

        Assert.Equal("GMV6 HR", RequiredString(service, "series"));
        Assert.Equal("ServiceManual", RequiredString(service, "sourceKind"));
        Assert.Equal("Imported", RequiredString(service, "importStatus"));
        Assert.Equal(262, RequiredInt(service, "entriesImported"));
        Assert.Equal("OwnerManual", RequiredString(owner, "sourceKind"));
        Assert.Equal("Analyzed", RequiredString(owner, "importStatus"));
        Assert.Equal(0, RequiredInt(owner, "entriesImported"));
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
