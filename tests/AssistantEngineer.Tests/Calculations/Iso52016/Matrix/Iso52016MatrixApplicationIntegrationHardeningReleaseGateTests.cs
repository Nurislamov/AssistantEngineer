using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixApplicationIntegrationHardeningReleaseGateTests
{
    [Fact]
    public void ReleaseManifest_ClosesApplicationIntegrationHardeningWithoutParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixApplicationIntegrationHardeningReleaseManifest.json");

        Assert.True(File.Exists(manifestPath), $"Release manifest was not found: {manifestPath}");

        var manifestText = File.ReadAllText(manifestPath);

        Assert.DoesNotContain("\"ExternalParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"FullParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"pyBuildingEnergyParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"EnergyPlusParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(manifestText);
        var root = document.RootElement;

        Assert.Equal("ISO52016-MATRIX-APPLICATION-INTEGRATION-HARDENING-RELEASE", root.GetProperty("stageId").GetString());
        Assert.Equal("ISO52016-MATRIX-APPLICATION-INTEGRATION-HARDENING", root.GetProperty("baseStageId").GetString());
        Assert.Equal("ApplicationIntegrationHardeningOnly", root.GetProperty("scope").GetString());
        Assert.True(root.GetProperty("buildingFacadeIntegrationIntegrated").GetBoolean());
        Assert.True(root.GetProperty("resultAggregationGuardsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("adjacentUnconditionedInputPathIntegrated").GetBoolean());
        Assert.True(root.GetProperty("annualReportAggregationGuardsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("stageGateIntegrated").GetBoolean());
        Assert.True(root.GetProperty("allInOneVerificationIntegrated").GetBoolean());
        Assert.True(root.GetProperty("releaseReadyGateIntegrated").GetBoolean());
        Assert.False(root.GetProperty("generatedArtifactsCommitted").GetBoolean());

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Application integration hardening only.", nonClaims);
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
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixApplicationIntegrationHardeningReleaseManifest.json");

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

        var applicationReleaseScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-application-integration-hardening-release-ready.ps1");

        var mainReleaseScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-release-ready.ps1");

        Assert.True(File.Exists(applicationReleaseScriptPath), $"Application integration hardening release-ready script was not found: {applicationReleaseScriptPath}");
        Assert.True(File.Exists(mainReleaseScriptPath), $"Main Matrix release-ready script was not found: {mainReleaseScriptPath}");

        var applicationReleaseScript = File.ReadAllText(applicationReleaseScriptPath);
        var mainReleaseScript = File.ReadAllText(mainReleaseScriptPath);

        Assert.Contains("ApplicationIntegrationHardeningOnly", applicationReleaseScript);
        Assert.Contains("verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1", applicationReleaseScript);
        Assert.Contains("assert-iso52016-matrix-application-integration-hardening-release-ready.ps1", mainReleaseScript);
    }

    [Fact]
    public void ReleaseDocumentation_KeepsClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(repoRoot, "docs", "calculations", "Iso52016MatrixApplicationIntegrationHardeningReleaseGate.md");

        Assert.True(File.Exists(docPath), $"Release gate doc was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("ApplicationIntegrationHardeningOnly", doc);
        Assert.Contains("Application integration hardening only.", doc);
        Assert.Contains("Validation anchors only, not full parity.", doc);
        Assert.Contains("No pyBuildingEnergy parity claim.", doc);
        Assert.Contains("No EnergyPlus parity claim.", doc);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", doc);
        Assert.Contains("No full ISO 52016 parity claim.", doc);
        Assert.Contains("does not require generated validation artifacts to be committed", doc);
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