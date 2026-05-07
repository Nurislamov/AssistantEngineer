using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1TraceabilityMatrixTests
{
    [Fact]
    public void TraceabilityMatrixFilesAndGeneratorExist()
    {
        var requiredFiles = new[]
        {
            TraceabilityJsonPath,
            TraceabilityMarkdownPath,
            TraceabilityReadmePath,
            GeneratorScriptPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required traceability matrix artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void TraceabilityMatrixDeclaresClosedV1AndSourceFiles()
    {
        using var matrix = ReadJson(TraceabilityJsonPath);
        var root = matrix.RootElement;

        Assert.Equal("Engineering Core V1 Traceability Matrix", root.GetProperty("matrixName").GetString());
        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("ClosedV1", root.GetProperty("status").GetString());

        var generatedFrom = ReadStringArray(root, "generatedFrom");

        Assert.Contains("docs/releases/EngineeringCoreV1Manifest.json", generatedFrom);
        Assert.Contains("docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json", generatedFrom);
        Assert.Contains("docs/validation/EnergyPlusValidationCaseRegistry.json", generatedFrom);
    }

    [Fact]
    public void TraceabilityClosedFormulaGatesMatchReleaseManifest()
    {
        using var matrix = ReadJson(TraceabilityJsonPath);
        using var manifest = ReadJson(ManifestPath);

        var matrixGates = matrix
            .RootElement
            .GetProperty("closedFormulaGates")
            .EnumerateArray()
            .Select(item => item.GetProperty("calculationId").GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var manifestGates = manifest
            .RootElement
            .GetProperty("closedFormulaGates")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(manifestGates, matrixGates);
    }

    [Fact]
    public void TraceabilityCountsMatchManifestDiagnosticsCatalogAndValidationRegistry()
    {
        using var matrix = ReadJson(TraceabilityJsonPath);
        using var manifest = ReadJson(ManifestPath);
        using var diagnostics = ReadJson(DiagnosticsCatalogPath);
        using var registry = ReadJson(ValidationRegistryPath);

        Assert.Equal(
            manifest.RootElement.GetProperty("closedFormulaGates").GetArrayLength(),
            matrix.RootElement.GetProperty("closedFormulaGateCount").GetInt32());

        Assert.Equal(
            diagnostics.RootElement.GetProperty("diagnostics").GetArrayLength(),
            matrix.RootElement.GetProperty("diagnosticsCount").GetInt32());

        Assert.Equal(
            registry.RootElement.GetProperty("cases").GetArrayLength(),
            matrix.RootElement.GetProperty("validationCaseCount").GetInt32());
    }

    [Fact]
    public void TraceabilityFormulaGateRowsExposeVisibilityAndDiagnosticsArray()
    {
        using var matrix = ReadJson(TraceabilityJsonPath);

        var gates = matrix
            .RootElement
            .GetProperty("closedFormulaGates")
            .EnumerateArray()
            .ToArray();

        Assert.NotEmpty(gates);

        foreach (var gate in gates)
        {
            Assert.False(string.IsNullOrWhiteSpace(gate.GetProperty("calculationId").GetString()));
            Assert.Equal("ClosedV1", gate.GetProperty("status").GetString());
            Assert.True(gate.GetProperty("diagnostics").ValueKind == JsonValueKind.Array);
            Assert.True(gate.GetProperty("apiVisible").GetBoolean());
            Assert.True(gate.GetProperty("reportDisclosureVisible").GetBoolean());
            Assert.True(gate.GetProperty("frontendVisible").GetBoolean());
        }
    }

    [Fact]
    public void TraceabilityContainsAnnual8760OutOfScopePlannedValidationEndpointFrontendBackendCiAndScripts()
    {
        using var matrix = ReadJson(TraceabilityJsonPath);
        var root = matrix.RootElement;

        Assert.Contains("EnergyDataSource = TrueHourlySimulation", ReadStringArray(root, "annual8760Requirements"));
        Assert.Contains("IsTrueHourly8760 = true", ReadStringArray(root, "annual8760Requirements"));
        Assert.Contains("HourlyRecordCount = 8760", ReadStringArray(root, "annual8760Requirements"));

        Assert.Contains("HVAC.LATENT_LOAD", ReadStringArray(root, "outOfScopeV1"));
        Assert.Contains("HVAC.MOISTURE_BALANCE", ReadStringArray(root, "outOfScopeV1"));

        Assert.Contains("VALIDATION.ENERGYPLUS_ASHRAE140", ReadStringArray(root, "plannedValidation"));

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/status",
            ReadStringArray(root, "applicationEndpoints"));

        Assert.Contains(
            "src/Frontend/src/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel.tsx",
            ReadStringArray(root, "frontendVisibility"));

        Assert.Contains(
            "src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx",
            ReadStringArray(root, "frontendVisibility"));

        Assert.Contains(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Facades/EngineeringCoreStatusFacade.cs",
            ReadStringArray(root, "backendVisibility"));

        Assert.Contains(
            ".github/workflows/engineering-core-v1.yml",
            ReadStringArray(root, "ciWorkflows"));

        Assert.Contains(
            "scripts/engineering-core/verify-engineering-core-v1.ps1",
            ReadStringArray(root, "verificationScripts"));
    }

    [Fact]
    public void TraceabilityValidationCasesMatchValidationRegistry()
    {
        using var matrix = ReadJson(TraceabilityJsonPath);
        using var registry = ReadJson(ValidationRegistryPath);

        var matrixCases = matrix
            .RootElement
            .GetProperty("validationCases")
            .EnumerateArray()
            .Select(item => item.GetProperty("caseId").GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var registryCases = registry
            .RootElement
            .GetProperty("cases")
            .EnumerateArray()
            .Select(item => item.GetProperty("caseId").GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(registryCases, matrixCases);
    }

    [Fact]
    public void TraceabilityMarkdownReadmeAndMatrixKeepNonClaimsVisible()
    {
        var combined = string.Join(
            Environment.NewLine,
            File.ReadAllText(TraceabilityMarkdownPath),
            File.ReadAllText(TraceabilityReadmePath),
            File.ReadAllText(TraceabilityJsonPath));

        var requiredNonClaims = new[]
        {
            "exact EnergyPlus numerical parity",
            "exact pyBuildingEnergy numerical parity",
            "ASHRAE 140 validation coverage",
            "full ISO 52016 node/matrix solver parity",
            "latent/moisture/humidity support in v1"
        };

        foreach (var requiredNonClaim in requiredNonClaims)
        {
            Assert.Contains(
                requiredNonClaim,
                combined,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TraceabilityGeneratorReadsManifestDiagnosticsValidationRegistryAndWritesJsonAndMarkdown()
    {
        var script = File.ReadAllText(GeneratorScriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCoreEvidence.csproj", script, StringComparison.Ordinal);
        Assert.Contains("generate-traceability-matrix", script, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", script, StringComparison.Ordinal);

        Assert.DoesNotContain("ConvertFrom-Json", script, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertTo-Json", script, StringComparison.Ordinal);

        var tool = ReadToolSourceBundle();

        Assert.Contains("EngineeringCoreV1Manifest.json", tool, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1DiagnosticsCatalog.json", tool, StringComparison.Ordinal);
        Assert.Contains("EnergyPlusValidationCaseRegistry.json", tool, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1TraceabilityMatrix.json", tool, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1TraceabilityMatrix.md", tool, StringComparison.Ordinal);
    }
private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string[] ReadStringArray(
        JsonElement root,
        string propertyName) =>
        root
            .GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

    private static string TraceabilityJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "traceability", "EngineeringCoreV1TraceabilityMatrix.json");

    private static string TraceabilityMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "traceability", "EngineeringCoreV1TraceabilityMatrix.md");

    private static string TraceabilityReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "traceability", "README.md");

    private static string GeneratorScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-traceability-matrix.ps1");

    private static string ManifestPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCoreV1Manifest.json");

    private static string DiagnosticsCatalogPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "EngineeringCoreV1DiagnosticsCatalog.json");

    private static string ValidationRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusValidationCaseRegistry.json");
    private static string ToolProgramPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EngineeringCoreEvidence",
            "Program.cs");

    private static string ToolRunnerPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EngineeringCoreEvidence",
            "EngineeringCoreEvidenceToolRunner.cs");

    private static string ReadToolSourceBundle() =>
        string.Join(
            Environment.NewLine,
            File.ReadAllText(ToolProgramPath),
            File.ReadAllText(ToolRunnerPath));
}

