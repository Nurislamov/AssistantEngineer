using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1ReleaseManifestTests
{
    [Fact]
    public void ReleaseManifestJsonExists()
    {
        Assert.True(
            File.Exists(ManifestJsonPath),
            $"Engineering Core V1 JSON manifest must exist: {ManifestJsonPath}");
    }

    [Fact]
    public void ReleaseManifestDocsExist()
    {
        var requiredDocs = new[]
        {
            ManifestMarkdownPath,
            ChecklistPath,
            OwnerHandoffPath
        };

        foreach (var requiredDoc in requiredDocs)
        {
            Assert.True(
                File.Exists(requiredDoc),
                $"Required release handoff document is missing: {requiredDoc}");
        }
    }

    [Fact]
    public void ManifestVerificationScriptExists()
    {
        Assert.True(
            File.Exists(ManifestVerificationScriptPath),
            $"Manifest verification script must exist: {ManifestVerificationScriptPath}");
    }

    [Fact]
    public void ManifestDeclaresClosedV1EngineeringFormulaGate()
    {
        using var manifest = ReadManifest();
        var root = manifest.RootElement;

        Assert.Equal(
            "AssistantEngineer Engineering Core",
            root.GetProperty("coreName").GetString());

        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("ClosedV1", root.GetProperty("status").GetString());
        Assert.Equal("engineering-formula-gate", root.GetProperty("releaseType").GetString());

        Assert.True(root.GetProperty("formulaGatesClosed").GetBoolean());
        Assert.True(root.GetProperty("weather8760GatesClosed").GetBoolean());
        Assert.True(root.GetProperty("annualHourly8760GateClosed").GetBoolean());
        Assert.True(root.GetProperty("successfulResultsMustNotContainErrorDiagnostics").GetBoolean());
    }

    [Fact]
    public void ManifestClosedFormulaGatesMatchFormulaAuditMatrixClosedV1Items()
    {
        using var manifest = ReadManifest();

        var manifestClosedGateIds = GetStringArray(
                manifest.RootElement,
                "closedFormulaGates")
            .Order(StringComparer.Ordinal)
            .ToArray();

        var matrixClosedGateIds = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.ClosedV1)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(matrixClosedGateIds, manifestClosedGateIds);
    }

    [Fact]
    public void ManifestOutOfScopeAndPlannedValidationMatchFormulaAuditMatrix()
    {
        using var manifest = ReadManifest();

        var manifestOutOfScope = GetStringArray(
                manifest.RootElement,
                "outOfScopeV1")
            .Order(StringComparer.Ordinal)
            .ToArray();

        var matrixOutOfScope = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.OutOfScopeV1)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(matrixOutOfScope, manifestOutOfScope);

        var manifestPlannedValidation = GetStringArray(
                manifest.RootElement,
                "plannedValidation")
            .Order(StringComparer.Ordinal)
            .ToArray();

        var matrixPlannedValidation = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.PlannedValidation)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(matrixPlannedValidation, manifestPlannedValidation);
    }

    [Fact]
    public void ManifestClosedFormulaGatesMatchEngineeringCoreStatusFacade()
    {
        using var manifest = ReadManifest();

        var manifestClosedGateIds = GetStringArray(
                manifest.RootElement,
                "closedFormulaGates")
            .Order(StringComparer.Ordinal)
            .ToArray();

        var facade = new EngineeringCoreStatusFacade();
        var status = facade.GetEngineeringCoreV1Status();

        Assert.True(status.IsSuccess, status.Error);

        var facadeClosedGateIds = status.Value.FormulaGates
            .Where(gate => gate.Status == "ClosedV1")
            .Select(gate => gate.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(facadeClosedGateIds, manifestClosedGateIds);
    }

    [Fact]
    public void ManifestContainsAnnual8760FlagsEndpointFrontendBackendDocsScriptsAndCiWorkflow()
    {
        using var manifest = ReadManifest();
        var root = manifest.RootElement;

        Assert.Contains(
            "EnergyDataSource = TrueHourlySimulation",
            GetStringArray(root, "requiredAnnual8760Flags"));

        Assert.Contains(
            "IsTrueHourly8760 = true",
            GetStringArray(root, "requiredAnnual8760Flags"));

        Assert.Contains(
            "HourlyRecordCount = 8760",
            GetStringArray(root, "requiredAnnual8760Flags"));

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/status",
            GetStringArray(root, "applicationEndpoints"));

        Assert.Contains(
            "src/Frontend/src/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel.tsx",
            GetStringArray(root, "frontendVisibility"));

        Assert.Contains(
            "src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx",
            GetStringArray(root, "frontendVisibility"));

        Assert.Contains(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Facades/EngineeringCoreStatusFacade.cs",
            GetStringArray(root, "backendVisibility"));

        Assert.Contains(
            "scripts/engineering-core/verify-engineering-core-v1.ps1",
            GetStringArray(root, "verificationScripts"));

        Assert.Contains(
            "scripts/engineering-core/verify-engineering-core-v1-manifest.ps1",
            GetStringArray(root, "verificationScripts"));

        Assert.Contains(
            ".github/workflows/engineering-core-v1.yml",
            GetStringArray(root, "ciWorkflows"));

        Assert.Contains(
            "docs/releases/EngineeringCoreV1Manifest.json",
            GetStringArray(root, "documentationFiles"));
    }

    [Fact]
    public void ManifestKeepsExplicitNonClaimsVisible()
    {
        using var manifest = ReadManifest();

        var nonClaims = GetStringArray(
            manifest.RootElement,
            "explicitNonClaims");

        var requiredNonClaims = new[]
        {
            "No exact pyBuildingEnergy numerical parity claim.",
            "No exact EnergyPlus numerical parity claim.",
            "No ASHRAE 140 validation coverage claim.",
            "No full ISO 52016 node/matrix solver parity claim.",
            "No latent/moisture/humidity calculation claim."
        };

        foreach (var requiredNonClaim in requiredNonClaims)
        {
            Assert.Contains(requiredNonClaim, nonClaims);
        }
    }

    [Fact]
    public void ReleaseManifestMarkdownChecklistAndOwnerHandoffKeepVerificationAndNonClaimsVisible()
    {
        var combined = string.Join(
            Environment.NewLine,
            File.ReadAllText(ManifestMarkdownPath),
            File.ReadAllText(ChecklistPath),
            File.ReadAllText(OwnerHandoffPath));

        Assert.Contains(
            ".\\scripts\\engineering-core\\verify-engineering-core-v1.ps1",
            combined,
            StringComparison.Ordinal);

        Assert.Contains(
            ".\\scripts\\engineering-core\\verify-engineering-core-v1-manifest.ps1",
            combined,
            StringComparison.Ordinal);

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/status",
            combined,
            StringComparison.Ordinal);

        Assert.Contains(
            "exact EnergyPlus",
            combined,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "ASHRAE 140",
            combined,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "full ISO 52016",
            combined,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManifestVerificationScriptRunsManifestDocsStatusDisclosureFrontendAndBuildChecks()
    {
        var content = File.ReadAllText(ManifestVerificationScriptPath);

        Assert.Contains(
            "EngineeringCoreV1ReleaseManifestTests",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreV1ProjectDocumentationTests",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreStatus",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreReportDisclosureTests",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreFrontendIntegrationGuardTests",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "npm --prefix .\\src\\Frontend run build",
            content,
            StringComparison.Ordinal);
    }

    private static JsonDocument ReadManifest() =>
        JsonDocument.Parse(File.ReadAllText(ManifestJsonPath));

    private static string[] GetStringArray(
        JsonElement root,
        string propertyName) =>
        root
            .GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

    private static string ManifestJsonPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCoreV1Manifest.json");

    private static string ManifestMarkdownPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCoreV1ReleaseManifest.md");

    private static string ChecklistPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCoreV1ReleaseChecklist.md");

    private static string OwnerHandoffPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCoreV1OwnerHandoff.md");

    private static string ManifestVerificationScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "verify-engineering-core-v1-manifest.ps1");
}
