using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalChainFinalReadinessTests
{
    [Fact]
    public void Manifest_DocumentsFinalReadinessRollupAndClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016PhysicalChainFinalReadinessManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-STEP-12", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-gate", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("matrixAllDiscoverabilityIntegrated").GetBoolean());

        var dependsOn = root
            .GetProperty("dependsOn")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        for (var step = 1; step <= 11; step++)
        {
            Assert.Contains($"AE-ISO52016-002-STEP-{step:00}", dependsOn);
        }

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Not full ISO 52016 parity.", claimBoundary);
        Assert.Contains("Not complete ISO 52016 numerical equivalence.", claimBoundary);
        Assert.Contains("Not pyBuildingEnergy parity.", claimBoundary);
        Assert.Contains("Not EnergyPlus parity.", claimBoundary);
        Assert.Contains("Not ASHRAE Standard 140 validation.", claimBoundary);
    }

    [Fact]
    public void TraceabilityMatrix_ListsPhysicalChainStagesAndGuards()
    {
        var repoRoot = FindRepositoryRoot();
        var traceabilityPath = Path.Combine(
            repoRoot,
            "docs",
            "traceability",
            "Iso52016PhysicalChainTraceabilityMatrix.json");

        Assert.True(File.Exists(traceabilityPath), $"Traceability matrix was not found: {traceabilityPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(traceabilityPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-002-PHYSICAL-CHAIN", root.GetProperty("traceabilityId").GetString());
        Assert.Equal("validation/internal engineering anchors only", root.GetProperty("claimBoundary").GetString());

        var workItems = root
            .GetProperty("workItems")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(11, workItems.Length);

        var ids = workItems
            .Select(item => item.GetProperty("id").GetString())
            .ToArray();

        Assert.Contains("AE-ISO52016-002-STEP-01", ids);
        Assert.Contains("AE-ISO52016-002-STEP-06", ids);
        Assert.Contains("AE-ISO52016-002-STEP-10", ids);
        Assert.Contains("AE-ISO52016-002-STEP-11", ids);

        var allText = File.ReadAllText(traceabilityPath);

        Assert.Contains("Iso52016PhysicalRoomModelBuilder", allText);
        Assert.Contains("Iso52016PhysicalRoomModelDiagnosticsBuilder", allText);
        Assert.Contains("AssistantEngineer.Tools.Iso52016PhysicalVerification", allText);
        Assert.Contains("ReducedMatrix default path", allText);
        Assert.Contains("PhysicalNodeModel explicit opt-in path", allText);
    }

    [Fact]
    public void FinalVerifier_DelegatesToReleaseGateAndRunsFinalGuardTests()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-physical-chain-final-ready.ps1");

        Assert.True(File.Exists(scriptPath), $"Final readiness script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("assert-iso52016-physical-model-chain-release-ready.ps1", script);
        Assert.Contains("Iso52016PhysicalChainFinalReadiness", script);
        Assert.Contains("validation/internal engineering anchors only", script);
        Assert.Contains("AE-ISO52016-002-STEP-12", script);
    }

    [Fact]
    public void MatrixAllVerificationScript_KeepsFinalReadinessDiscoverable()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        Assert.True(File.Exists(scriptPath), $"Matrix all-verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("assert-iso52016-physical-chain-final-ready.ps1", script);
        Assert.Contains("Iso52016PhysicalChainFinalReadinessManifest.json", script);
        Assert.Contains("Iso52016PhysicalChainTraceabilityMatrix.json", script);
        Assert.Contains("AE-ISO52016-002 Step 12 physical chain final readiness", script);
    }

    [Fact]
    public void FinalReadinessDocumentation_StatesHonestClaimBoundary()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016PhysicalChainFinalReadiness.md");

        Assert.True(File.Exists(docPath), $"Final readiness document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("AE-ISO52016-002", doc);
        Assert.Contains("ISO52016-inspired", doc);
        Assert.Contains("validation/internal engineering anchors only", doc);
        Assert.Contains("not full ISO 52016 parity", doc);
        Assert.Contains("not complete ISO 52016 numerical equivalence", doc);
        Assert.Contains("not pyBuildingEnergy parity", doc);
        Assert.Contains("not EnergyPlus parity", doc);
        Assert.Contains("not ASHRAE Standard 140 validation", doc);
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