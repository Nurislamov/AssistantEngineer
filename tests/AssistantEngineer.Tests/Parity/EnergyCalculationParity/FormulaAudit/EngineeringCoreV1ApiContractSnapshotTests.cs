using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1ApiContractSnapshotTests
{
    [Fact]
    public void ApiContractSnapshotFilesExist()
    {
        var requiredFiles = new[]
        {
            StatusSnapshotPath,
            DiagnosticsSnapshotPath,
            HttpFilePath,
            ReadmePath,
            GeneratorScriptPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required Engineering Core V1 API contract file is missing: {requiredFile}");
        }
    }

    [Fact]
    public void StatusSnapshotDeclaresClosedV1AndRequiredBooleanGates()
    {
        using var status = ReadJson(StatusSnapshotPath);
        var root = status.RootElement;

        Assert.Equal("AssistantEngineer Engineering Core", root.GetProperty("coreName").GetString());
        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("ClosedV1", root.GetProperty("status").GetString());

        Assert.True(root.GetProperty("formulaGatesClosed").GetBoolean());
        Assert.True(root.GetProperty("weather8760GatesClosed").GetBoolean());
        Assert.True(root.GetProperty("annualHourly8760GateClosed").GetBoolean());
        Assert.True(root.GetProperty("successfulResultsMustNotContainErrorDiagnostics").GetBoolean());
    }

    [Fact]
    public void StatusSnapshotClosedGatesMatchReleaseManifest()
    {
        using var status = ReadJson(StatusSnapshotPath);
        using var manifest = ReadJson(ManifestPath);

        var snapshotGateIds = status
            .RootElement
            .GetProperty("formulaGates")
            .EnumerateArray()
            .Select(item => item.GetProperty("calculationId").GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var manifestGateIds = manifest
            .RootElement
            .GetProperty("closedFormulaGates")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(manifestGateIds, snapshotGateIds);
    }

    [Fact]
    public void StatusSnapshotContainsAnnual8760FlagsOutOfScopePlannedValidationDocsAndNonClaims()
    {
        using var status = ReadJson(StatusSnapshotPath);
        var root = status.RootElement;

        Assert.Contains(
            "EnergyDataSource = TrueHourlySimulation",
            ReadStringArray(root, "requiredAnnual8760Flags"));

        Assert.Contains(
            "IsTrueHourly8760 = true",
            ReadStringArray(root, "requiredAnnual8760Flags"));

        Assert.Contains(
            "HourlyRecordCount = 8760",
            ReadStringArray(root, "requiredAnnual8760Flags"));

        Assert.Contains(
            "HVAC.LATENT_LOAD",
            ReadStringArray(root, "outOfScopeV1"));

        Assert.Contains(
            "HVAC.MOISTURE_BALANCE",
            ReadStringArray(root, "outOfScopeV1"));

        Assert.Contains(
            "VALIDATION.ENERGYPLUS_ASHRAE140",
            ReadStringArray(root, "plannedValidation"));

        Assert.Contains(
            "docs/calculations/EngineeringCoreV1Scope.md",
            ReadStringArray(root, "documentationFiles"));

        var nonClaims = ReadStringArray(root, "explicitNonClaims");

        Assert.Contains(
            "No exact EnergyPlus numerical parity claim.",
            nonClaims);

        Assert.Contains(
            "No exact pyBuildingEnergy numerical parity claim.",
            nonClaims);

        Assert.Contains(
            "No ASHRAE 140 validation coverage claim.",
            nonClaims);
    }

    [Fact]
    public void DiagnosticsSnapshotDeclaresRulesAndMatchesDiagnosticsCatalogCodes()
    {
        using var snapshot = ReadJson(DiagnosticsSnapshotPath);
        using var catalog = ReadJson(DiagnosticsCatalogPath);

        Assert.Equal(
            "Engineering Core V1 Diagnostics Catalog",
            snapshot.RootElement.GetProperty("catalogName").GetString());

        Assert.Equal("v1", snapshot.RootElement.GetProperty("version").GetString());
        Assert.Equal("ClosedV1", snapshot.RootElement.GetProperty("status").GetString());

        Assert.Contains(
            "CalculationDiagnosticSeverity.Error",
            snapshot.RootElement
                .GetProperty("rules")
                .GetProperty("successRule")
                .GetString(),
            StringComparison.Ordinal);

        var snapshotCodes = snapshot
            .RootElement
            .GetProperty("diagnostics")
            .EnumerateArray()
            .Select(item => item.GetProperty("code").GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var catalogCodes = catalog
            .RootElement
            .GetProperty("diagnostics")
            .EnumerateArray()
            .Select(item => item.GetProperty("code").GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(catalogCodes, snapshotCodes);
    }

    [Fact]
    public void DiagnosticsSnapshotItemsContainUserMessageUserActionAndClosedGate()
    {
        using var snapshot = ReadJson(DiagnosticsSnapshotPath);

        var diagnostics = snapshot
            .RootElement
            .GetProperty("diagnostics")
            .EnumerateArray()
            .ToArray();

        Assert.NotEmpty(diagnostics);

        foreach (var diagnostic in diagnostics)
        {
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.GetProperty("code").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.GetProperty("severity").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.GetProperty("category").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.GetProperty("userMessage").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.GetProperty("userAction").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.GetProperty("closedV1Gate").GetString()));
        }
    }

    [Fact]
    public void HttpFileDocumentsBothEngineeringCoreEndpoints()
    {
        var content = File.ReadAllText(HttpFilePath);

        Assert.Contains(
            "GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/diagnostics-catalog",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Accept: application/json",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadmeDocumentsGenerationVerificationContractRulesAndNonClaims()
    {
        var content = File.ReadAllText(ReadmePath);

        Assert.Contains(
            "generate-engineering-core-v1-api-contract-snapshots.ps1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreV1ApiContractSnapshotTests",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "no exact EnergyPlus numerical parity",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "no ASHRAE 140 validation coverage",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GeneratorScriptReadsManifestAndDiagnosticsCatalogAndWritesSnapshots()
    {
        var content = File.ReadAllText(GeneratorScriptPath);

        Assert.Contains("EngineeringCoreV1Manifest.json", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1DiagnosticsCatalog.json", content, StringComparison.Ordinal);
        Assert.Contains("status.sample.json", content, StringComparison.Ordinal);
        Assert.Contains("diagnostics-catalog.sample.json", content, StringComparison.Ordinal);
        Assert.Contains("engineering-core-v1.http", content, StringComparison.Ordinal);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string[] ReadStringArray(JsonElement root, string propertyName) =>
        root
            .GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

    private static string StatusSnapshotPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "status.sample.json");

    private static string DiagnosticsSnapshotPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "diagnostics-catalog.sample.json");

    private static string HttpFilePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "engineering-core-v1.http");

    private static string ReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "README.md");

    private static string GeneratorScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-api-contract-snapshots.ps1");

    private static string ManifestPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCoreV1Manifest.json");

    private static string DiagnosticsCatalogPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "EngineeringCoreV1DiagnosticsCatalog.json");
}
