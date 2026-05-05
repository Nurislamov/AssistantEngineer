using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalSelectionApplicationIntegrationHardeningTests
{
    [Fact]
    public void Manifest_DocumentsApplicationIntegrationHardeningAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalSelectionApplicationIntegrationManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-13", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-gate", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("matrixAllVerificationIntegrated").GetBoolean());

        var closedWorkItems = root
            .GetProperty("closedWorkItems")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("AE-ISO52016-002", closedWorkItems);

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Physical model selection application integration hardening stage.", claimBoundary);
        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not full ISO 52016 parity.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy parity.", claimBoundary);
        Assert.Contains("Not EnergyPlus parity.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 validation.", claimBoundary);
    }

    [Fact]
    public void SelectionLayer_KeepsReducedMatrixDefaultAndPhysicalExplicitOptIn()
    {
        var repoRoot = FindRepositoryRoot();
        var contractsRoot = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Contracts",
            "Iso52016",
            "Physical");

        var servicePath = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Physical",
            "Iso52016PhysicalModelSelectionService.cs");

        var strategyPath = Path.Combine(contractsRoot, "Iso52016PhysicalModelSelectionStrategy.cs");
        var requestPath = Path.Combine(contractsRoot, "Iso52016PhysicalModelSelectionRequest.cs");

        Assert.True(File.Exists(strategyPath), $"Strategy contract was not found: {strategyPath}");
        Assert.True(File.Exists(requestPath), $"Selection request contract was not found: {requestPath}");
        Assert.True(File.Exists(servicePath), $"Selection service was not found: {servicePath}");

        var strategyText = File.ReadAllText(strategyPath);
        var requestText = File.ReadAllText(requestPath);
        var serviceText = File.ReadAllText(servicePath);

        Assert.Contains("ReducedMatrix", strategyText);
        Assert.Contains("PhysicalNodeModel", strategyText);
        Assert.Contains("ReducedMatrix", requestText);
        Assert.Contains("IIso52016MatrixReducedRoomModelBuilder", serviceText);
        Assert.Contains("IIso52016PhysicalRoomModelBuilder", serviceText);
        Assert.Contains("IIso52016MatrixHourlySolver", serviceText);
        Assert.Contains("PhysicalNodeModel", serviceText);
    }

    [Fact]
    public void Documentation_StatesExplicitOptInAndNoExternalParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalSelectionApplicationIntegration.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("ReducedMatrix remains the default", doc);
        Assert.Contains("PhysicalNodeModel is explicit opt-in", doc);
        Assert.Contains("validation/internal engineering anchors only", doc);
        Assert.Contains("not full ISO 52016 parity", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not pyBuildingEnergy parity", doc);
        Assert.Contains("not EnergyPlus parity", doc);
        Assert.Contains("not ASHRAE Standard 140 validation", doc);

        Assert.DoesNotContain("full ISO 52016 parity achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnergyPlus parity achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pyBuildingEnergy parity achieved", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ASHRAE Standard 140 validation passed", doc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerificationScript_RunsApplicationIntegrationHardeningGuardTests()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-physical-selection-application-integration-hardening.ps1");

        Assert.True(File.Exists(scriptPath), $"Verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("Iso52016PhysicalSelectionApplicationIntegrationHardeningTests", script);
        Assert.Contains("Iso52016PhysicalSelectionApplicationIntegrationManifest.json", script);
        Assert.Contains("validation/internal engineering anchors only", script);
    }

    [Fact]
    public void MatrixAllVerificationScript_ContainsStep13DiscoverabilityHook()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        Assert.True(File.Exists(scriptPath), $"Matrix all-verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("verify-iso52016-physical-selection-application-integration-hardening.ps1", script);
        Assert.Contains("Iso52016PhysicalSelectionApplicationIntegrationManifest.json", script);
        Assert.Contains("ReducedMatrix remains the default application-facing path", script);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(
                directory.FullName,
                "src",
                "Backend",
                "AssistantEngineer.Modules.Calculations");

            var tests = Path.Combine(
                directory.FullName,
                "tests",
                "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }
}