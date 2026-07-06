using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeDiagnosticManualRegistryReconciliationTests
{
    private static readonly string RegistryPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "manual-library",
        "manuals.json");

    [Theory]
    [InlineData("gree-umatch-r32-service-manual", 107, "DeployedAndSmokeVerified")]
    [InlineData("gree-gmv9-flex-service-manual-2025-12", 260, "DeployedAndSmokeVerified")]
    [InlineData("gree-gmv-x-service-manual-2022-09", 263, "DeployedAndSmokeVerified")]
    [InlineData("gree-gmv6-hr-service-manual-2025-07", 262, "DeployedAndSmokeVerified")]
    [InlineData("gree-erv-b-series-service-manual", 2, "DeployedAndSmokeVerified")]
    [InlineData("gree-erv-wired-controller-owner-manual", 3, "DeployedAndSmokeVerified")]
    [InlineData("gree-gmv6-service-manual-2022-03", 8, "DeployedAndSmokeVerified")]
    public void RegistryMatchesProvenImportAndProductionState(
        string manualId,
        int entriesImported,
        string productionStatus)
    {
        var registry = ReadObject(RegistryPath);
        var manual = Assert.Single(
            RequiredArray(registry, "manuals").OfType<JsonObject>(),
            item => RequiredString(item, "manualId") == manualId);

        Assert.Equal(entriesImported, RequiredInt(manual, "entriesImported"));
        Assert.Equal(productionStatus, RequiredString(manual, "productionStatus"));
        Assert.NotEqual("NotImported", RequiredString(manual, "productionStatus"));
    }

    [Fact]
    public void IntermediateProductionStatusesAreDeclaredByRegistrySchema()
    {
        var registry = ReadObject(RegistryPath);
        var statusModels = Assert.IsType<JsonObject>(registry["statusModels"]);
        var statuses = RequiredArray(statusModels, "productionStatus")
            .Select(value => Assert.IsAssignableFrom<JsonValue>(value).GetValue<string>())
            .ToArray();

        Assert.Contains("RuntimeImported", statuses);
        Assert.Contains("DeltaImportedPendingProductionSmoke", statuses);
        Assert.Contains("DeployedAndSmokeVerified", statuses);
    }

    [Fact]
    public void ErvInstallationManualRemainsAnalyzedWithoutClaimingRuntimeImport()
    {
        var registry = ReadObject(RegistryPath);
        var manual = Assert.Single(
            RequiredArray(registry, "manuals").OfType<JsonObject>(),
            item => RequiredString(item, "manualId") ==
                    "gree-erv-b-series-installation-startup-maintenance");

        Assert.Equal("Analyzed", RequiredString(manual, "importStatus"));
        Assert.Equal("DiagnosticSectionsIdentified", RequiredString(manual, "coverageStatus"));
        Assert.Equal(0, RequiredInt(manual, "entriesImported"));
        Assert.Equal("NotImported", RequiredString(manual, "productionStatus"));
    }

    private static JsonObject ReadObject(string path) =>
        Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));

    private static JsonArray RequiredArray(JsonObject node, string property) =>
        Assert.IsType<JsonArray>(node[property]);

    private static string RequiredString(JsonObject node, string property) =>
        Assert.IsAssignableFrom<JsonValue>(node[property]).GetValue<string>();

    private static int RequiredInt(JsonObject node, string property) =>
        Assert.IsAssignableFrom<JsonValue>(node[property]).GetValue<int>();
}
