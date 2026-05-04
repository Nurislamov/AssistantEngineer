using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixEngineeringEdgeCasesReleaseGateTests
{
    [Fact]
    public void ReleaseManifest_ClosesEngineeringEdgeCasesWithoutParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixEngineeringEdgeCasesReleaseManifest.json");

        Assert.True(File.Exists(manifestPath), $"Release manifest was not found: {manifestPath}");

        var manifestText = File.ReadAllText(manifestPath);

        Assert.DoesNotContain("\"ExternalParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"FullParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"pyBuildingEnergyParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"EnergyPlusParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(manifestText);
        var root = document.RootElement;

        Assert.Equal("ISO52016-MATRIX-ENGINEERING-EDGE-CASES-RELEASE", root.GetProperty("stageId").GetString());
        Assert.Equal("ISO52016-MATRIX-ENGINEERING-EDGE-CASES", root.GetProperty("baseStageId").GetString());
        Assert.Equal("EngineeringHardeningOnly", root.GetProperty("scope").GetString());
        Assert.True(root.GetProperty("multiNodeThermalResponseIntegrated").GetBoolean());
        Assert.True(root.GetProperty("adjacentUnconditionedBoundaryIntegrated").GetBoolean());
        Assert.True(root.GetProperty("timeStepScalingIntegrated").GetBoolean());
        Assert.True(root.GetProperty("signConventionGuardsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("annualMonthlyAggregationGuardsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("stageGateIntegrated").GetBoolean());
        Assert.True(root.GetProperty("allInOneVerificationIntegrated").GetBoolean());
        Assert.True(root.GetProperty("releaseReadyGateIntegrated").GetBoolean());
        Assert.True(root.GetProperty("mergeEvidenceIntegrated").GetBoolean());
        Assert.False(root.GetProperty("generatedArtifactsCommitted").GetBoolean());

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Engineering edge-case hardening only.", nonClaims);
        Assert.Contains("Validation anchors only, not full parity.", nonClaims);
        Assert.Contains("No pyBuildingEnergy parity claim.", nonClaims);
        Assert.Contains("No EnergyPlus parity claim.", nonClaims);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", nonClaims);
        Assert.Contains("No full ISO 52016 parity claim.", nonClaims);
        Assert.Contains("No ExternalParityCovered claim.", nonClaims);
        Assert.Contains("No FullParityCovered claim.", nonClaims);
    }

    [Fact]
    public void ReleaseManifest_ReferencesExistingFiles()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixEngineeringEdgeCasesReleaseManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        AssertExistingPaths(repoRoot, root.GetProperty("requiredManifests"));
        AssertExistingPaths(repoRoot, root.GetProperty("documentationFiles"));
        AssertExistingPaths(repoRoot, root.GetProperty("verificationScripts"));
        AssertExistingPaths(repoRoot, root.GetProperty("testGuards"));
    }

    [Fact]
    public void ReleaseReadyScript_IsConnectedToMainMatrixReleaseReadyGate()
    {
        var repoRoot = FindRepositoryRoot();

        var edgeReleaseScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1");

        var mainReleaseScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-release-ready.ps1");

        Assert.True(File.Exists(edgeReleaseScriptPath), $"Engineering edge-case release-ready script was not found: {edgeReleaseScriptPath}");
        Assert.True(File.Exists(mainReleaseScriptPath), $"Main Matrix release-ready script was not found: {mainReleaseScriptPath}");

        var edgeReleaseScript = File.ReadAllText(edgeReleaseScriptPath);
        var mainReleaseScript = File.ReadAllText(mainReleaseScriptPath);

        Assert.Contains("EngineeringHardeningOnly", edgeReleaseScript);
        Assert.Contains("verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1", edgeReleaseScript);
        Assert.Contains("assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1", mainReleaseScript);
    }

    [Fact]
    public void StageGate_IsConnectedToAllInOneVerification()
    {
        var repoRoot = FindRepositoryRoot();

        var stageGatePath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1");

        var allScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        Assert.True(File.Exists(stageGatePath), $"Engineering edge-case stage gate was not found: {stageGatePath}");
        Assert.True(File.Exists(allScriptPath), $"Main Matrix all verification script was not found: {allScriptPath}");

        var stageGate = File.ReadAllText(stageGatePath);
        var allScript = File.ReadAllText(allScriptPath);

        Assert.Contains("EngineeringHardeningOnly", stageGate);
        Assert.Contains("verify-iso52016-matrix-engineering-edge-cases.ps1", stageGate);
        Assert.Contains("verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1", allScript);
    }

    [Fact]
    public void ReleaseDocumentation_KeepsClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(repoRoot, "docs", "calculations", "Iso52016MatrixEngineeringEdgeCasesReleaseGate.md");

        Assert.True(File.Exists(docPath), $"Release gate doc was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("Engineering edge-case hardening only.", doc);
        Assert.Contains("Validation anchors only, not full parity.", doc);
        Assert.Contains("No pyBuildingEnergy parity claim.", doc);
        Assert.Contains("No EnergyPlus parity claim.", doc);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", doc);
        Assert.Contains("No full ISO 52016 parity claim.", doc);
        Assert.Contains("must not be committed", doc);
    }

    private static void AssertExistingPaths(string repoRoot, JsonElement relativePaths)
    {
        foreach (var item in relativePaths.EnumerateArray())
        {
            var relativePath = item.GetString();
            Assert.False(string.IsNullOrWhiteSpace(relativePath));

            var path = Path.Combine(relativePath!.Split('/').Prepend(repoRoot).ToArray());
            Assert.True(File.Exists(path), $"Manifest path was not found: {relativePath}");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(directory.FullName, "src", "Backend", "AssistantEngineer.Modules.Calculations");
            var tests = Path.Combine(directory.FullName, "tests", "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate AssistantEngineer repository root from test base directory.");
    }
}