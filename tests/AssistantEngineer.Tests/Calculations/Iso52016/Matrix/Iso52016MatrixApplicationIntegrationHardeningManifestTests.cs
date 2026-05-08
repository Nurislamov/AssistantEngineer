using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixApplicationIntegrationHardeningManifestTests
{
    [Fact]
    public void ApplicationIntegrationHardeningManifest_ListsExistingFixturesDocsScriptsAndTests()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixApplicationIntegrationHardeningManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal(
            "ISO52016-MATRIX-APPLICATION-INTEGRATION-HARDENING",
            root.GetProperty("stageId").GetString());

        Assert.Equal(
            "ApplicationIntegrationHardening",
            root.GetProperty("scope").GetString());

        Assert.Equal(
            "ValidationAnchorOnly",
            root.GetProperty("claimScope").GetString());

        Assert.Equal(
            5,
            root.GetProperty("fixtureCount").GetInt32());

        Assert.True(root.GetProperty("facadeAggregationAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("domainMappingAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("resultContractAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("reportAggregationAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("verificationScriptIntegrated").GetBoolean());
        Assert.True(root.GetProperty("allInOneVerificationIntegrated").GetBoolean());

        AssertExistingPaths(repoRoot, root.GetProperty("fixtures"));
        AssertExistingPaths(repoRoot, root.GetProperty("documentationFiles"));
        AssertExistingPaths(repoRoot, root.GetProperty("verificationScripts"));
        AssertExistingPaths(repoRoot, root.GetProperty("testGuards"));
    }

    [Fact]
    public void ApplicationIntegrationHardeningDocs_KeepClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();
        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixApplicationIntegrationHardening.md");

        Assert.True(File.Exists(docPath), $"Documentation was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("Application integration hardening only.", doc);
        Assert.Contains("Validation anchors only, not full equivalence claim.", doc);
        Assert.Contains("No StandardReference equivalence claim.", doc);
        Assert.Contains("No EnergyPlus comparison workflow claim.", doc);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor coverage claim.", doc);
        Assert.Contains("No full ISO 52016 equivalence claim.", doc);
        Assert.Contains("ManualEngineeringIntegrationAnchor", doc);
    }

    [Fact]
    public void ApplicationIntegrationHardeningVerification_IsConnectedThroughRegistry()
    {
        var repoRoot = FindRepositoryRoot();

        var verifyScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-application-integration-hardening.ps1");

        Assert.True(File.Exists(verifyScriptPath), $"Verification script was not found: {verifyScriptPath}");

        var verifyScript = File.ReadAllText(verifyScriptPath);

        Assert.Contains("ApplicationIntegrationHardening", verifyScript);
        Assert.Contains("ManualEngineeringIntegrationAnchor", verifyScript);
        Assert.Contains("Iso52016MatrixApplicationIntegrationHardening", verifyScript);
        RegistryContainsStageFile(
            "ISO52016-MATRIX-APPLICATION-INTEGRATION-HARDENING",
            "relatedManifests",
            "docs/releases/Iso52016MatrixApplicationIntegrationHardeningManifest.json");
    }

    private static void AssertExistingPaths(
        string repoRoot,
        JsonElement relativePaths)
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
