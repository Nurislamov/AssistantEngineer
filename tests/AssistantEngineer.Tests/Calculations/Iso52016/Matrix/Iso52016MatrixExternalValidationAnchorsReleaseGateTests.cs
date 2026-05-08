using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixExternalValidationAnchorsReleaseGateTests
{
    [Fact]
    public void ReleaseManifest_ClosesExternalValidationAnchorsWithoutFullExternalExactMatchClaims()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixExternalValidationAnchorsReleaseManifest.json");

        Assert.True(File.Exists(manifestPath), $"Release manifest was not found: {manifestPath}");

        var manifestText = File.ReadAllText(manifestPath);

        Assert.DoesNotContain("\"ExternalReferenceCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"FullReferenceCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"StandardReferenceCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"EnergyPlusComparisonCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(manifestText);
        var root = document.RootElement;

        Assert.Equal("ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS-RELEASE", root.GetProperty("stageId").GetString());
        Assert.Equal("ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS", root.GetProperty("baseStageId").GetString());
        Assert.Equal("ValidationAnchorOnly", root.GetProperty("scope").GetString());
        Assert.True(root.GetProperty("simpleIndependentManualAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("annual8760ManualReferenceIntegrated").GetBoolean());
        Assert.True(root.GetProperty("StandardReferenceStyleNamingIntegrated").GetBoolean());
        Assert.True(root.GetProperty("energyPlusStyleNamingIntegrated").GetBoolean());
        Assert.True(root.GetProperty("stageGateIntegrated").GetBoolean());
        Assert.True(root.GetProperty("allInOneVerificationIntegrated").GetBoolean());
        Assert.True(root.GetProperty("releaseReadyGateIntegrated").GetBoolean());
        Assert.False(root.GetProperty("generatedArtifactsCommitted").GetBoolean());

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
    public void ReleaseManifest_ReferencesExistingFiles()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixExternalValidationAnchorsReleaseManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        AssertExistingPaths(repoRoot, root.GetProperty("requiredManifests"));
        AssertExistingPaths(repoRoot, root.GetProperty("documentationFiles"));
        AssertExistingPaths(repoRoot, root.GetProperty("verificationScripts"));
        AssertExistingPaths(repoRoot, root.GetProperty("testGuards"));
    }

    [Fact]
    public void ReleaseReadyStage_IsConnectedThroughVerificationRegistry()
    {
        var repoRoot = FindRepositoryRoot();

        var externalReleaseScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-external-validation-anchors-release-ready.ps1");

        Assert.True(File.Exists(externalReleaseScriptPath), $"External validation anchors release-ready script was not found: {externalReleaseScriptPath}");

        var externalReleaseScript = File.ReadAllText(externalReleaseScriptPath);

        Assert.Contains("ValidationAnchorOnly", externalReleaseScript);
        Assert.Contains("verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1", externalReleaseScript);
        RegistryContainsStageFile(
            "ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS",
            "relatedManifests",
            "docs/releases/Iso52016MatrixExternalValidationAnchorsReleaseManifest.json");
    }

    [Fact]
    public void ReleaseDocumentation_KeepsClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(repoRoot, "docs", "calculations", "Iso52016MatrixExternalValidationAnchorsReleaseGate.md");

        Assert.True(File.Exists(docPath), $"Release gate doc was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("Validation anchors only, not full equivalence claim.", doc);
        Assert.Contains("IndependentManualEngineeringFormula", doc);
        Assert.Contains("No exact StandardReference numerical equivalence claim.", doc);
        Assert.Contains("No exact EnergyPlus numerical equivalence claim.", doc);
        Assert.Contains("No ExternalReferenceCovered claim.", doc);
        Assert.Contains("No FullReferenceCovered claim.", doc);
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
