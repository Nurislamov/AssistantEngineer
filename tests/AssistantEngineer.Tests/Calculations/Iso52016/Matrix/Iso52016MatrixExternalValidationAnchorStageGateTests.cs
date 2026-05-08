using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixExternalValidationAnchorStageGateTests
{
    [Fact]
    public void StageGateManifest_DocumentsEveryLayerWithoutFullParityClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixExternalValidationAnchorsStageGateManifest.json");

        Assert.True(File.Exists(manifestPath), $"Stage-gate manifest was not found: {manifestPath}");

        var manifestText = File.ReadAllText(manifestPath);

        Assert.DoesNotContain("\"ExternalReferenceCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"FullReferenceCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"StandardReferenceCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"EnergyPlusComparisonCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);

        using var manifest = JsonDocument.Parse(manifestText);
        var root = manifest.RootElement;

        Assert.Equal("ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS-STAGE-GATE", root.GetProperty("stageId").GetString());
        Assert.Equal("ValidationAnchorOnly", root.GetProperty("scope").GetString());
        Assert.True(root.GetProperty("simpleIndependentManualAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("annual8760ManualReferenceIntegrated").GetBoolean());
        Assert.True(root.GetProperty("StandardReferenceStyleNamingIntegrated").GetBoolean());
        Assert.True(root.GetProperty("energyPlusStyleNamingIntegrated").GetBoolean());
        Assert.True(root.GetProperty("allInOneVerificationIntegrated").GetBoolean());
        Assert.True(root.GetProperty("releaseReadyGateIntegrated").GetBoolean());

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Validation anchors only, not full equivalence claim.", nonClaims);
        Assert.Contains("No exact StandardReference numerical equivalence claim.", nonClaims);
        Assert.Contains("No exact EnergyPlus numerical equivalence claim.", nonClaims);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor coverage claim.", nonClaims);
        Assert.Contains("No ExternalReferenceCovered claim.", nonClaims);
        Assert.Contains("No FullReferenceCovered claim.", nonClaims);
    }

    [Fact]
    public void StageGateManifest_ReferencesExistingFiles()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixExternalValidationAnchorsStageGateManifest.json");

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = manifest.RootElement;

        AssertExistingPaths(repoRoot, root.GetProperty("requiredManifests"));
        AssertExistingPaths(repoRoot, root.GetProperty("documentationFiles"));
        AssertExistingPaths(repoRoot, root.GetProperty("verificationScripts"));
        AssertExistingPaths(repoRoot, root.GetProperty("testGuards"));
    }

    [Fact]
    public void StageGate_IsConnectedThroughVerificationRegistry()
    {
        var repoRoot = FindRepositoryRoot();

        var stageGateScriptPath = Path.Combine(repoRoot, "scripts", "iso52016", "verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1");
        Assert.True(File.Exists(stageGateScriptPath), $"Stage-gate verification script was not found: {stageGateScriptPath}");

        Assert.Contains("ValidationAnchorOnly", File.ReadAllText(stageGateScriptPath));
        RegistryContainsStageFile(
            "ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS",
            "relatedManifests",
            "docs/releases/Iso52016MatrixExternalValidationAnchorsStageGateManifest.json");
    }

    [Fact]
    public void StageGateDocumentation_KeepsClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(repoRoot, "docs", "calculations", "Iso52016MatrixExternalValidationAnchorsStageGate.md");

        Assert.True(File.Exists(docPath), $"Stage-gate doc was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("Validation anchors only, not full equivalence claim.", doc);
        Assert.Contains("No exact StandardReference numerical equivalence claim.", doc);
        Assert.Contains("No exact EnergyPlus numerical equivalence claim.", doc);
        Assert.Contains("No ExternalReferenceCovered claim.", doc);
        Assert.Contains("No FullReferenceCovered claim.", doc);
        Assert.Contains("not a full external equivalence gate", doc);
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
