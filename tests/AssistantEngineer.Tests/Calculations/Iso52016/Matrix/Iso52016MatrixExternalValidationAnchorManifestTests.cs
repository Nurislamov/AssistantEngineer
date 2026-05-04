using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixExternalValidationAnchorManifestTests
{
    [Fact]
    public void ExternalValidationAnchorManifest_ListsEveryFixtureFileOnDisk()
    {
        var repoRoot = FindRepositoryRoot();
        var anchorDirectory = Path.Combine(
            repoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "ExternalValidationAnchors");

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnchorsManifest.json");

        Assert.True(Directory.Exists(anchorDirectory), $"Anchor directory was not found: {anchorDirectory}");
        Assert.True(File.Exists(manifestPath), $"Anchor manifest was not found: {manifestPath}");

        using var manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifestRoot = manifestDocument.RootElement;

        Assert.Equal(
            "ValidationAnchorOnly",
            manifestRoot.GetProperty("claimScope").GetString());

        var manifestFixtures = manifestRoot
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => item is not null)
            .Cast<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var diskFixtures = Directory
            .GetFiles(anchorDirectory, "*.json")
            .Select(file => Path.GetRelativePath(repoRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(diskFixtures.Length >= 10, $"Expected at least 10 external validation anchor fixtures, found {diskFixtures.Length}.");

        foreach (var diskFixture in diskFixtures)
        {
            Assert.Contains(diskFixture, manifestFixtures);
        }
    }

    [Fact]
    public void ExternalValidationAnchorFixtures_HaveUniqueIdsAndValidationAnchorOnlyScope()
    {
        var repoRoot = FindRepositoryRoot();
        var anchorDirectory = Path.Combine(
            repoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "ExternalValidationAnchors");

        var anchorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fixturePath in Directory.GetFiles(anchorDirectory, "*.json"))
        {
            using var fixtureDocument = JsonDocument.Parse(File.ReadAllText(fixturePath));
            var root = fixtureDocument.RootElement;

            var anchorId = root.GetProperty("anchorId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(anchorId));
            Assert.True(anchorIds.Add(anchorId), $"Duplicate anchor id: {anchorId}");

            Assert.Equal(
                "ValidationAnchorOnly",
                root.GetProperty("claimScope").GetString());

            var nonClaims = root
                .GetProperty("explicitNonClaims")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();

            Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", nonClaims);
            Assert.Contains("No exact EnergyPlus numerical parity claim.", nonClaims);
            Assert.Contains("No ASHRAE 140 validation coverage claim.", nonClaims);
        }
    }

    [Fact]
    public void ExternalValidationAnchorVerificationScript_GuardsManifestCompletenessAndFixtureCount()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-external-validation-anchors.ps1");

        Assert.True(File.Exists(scriptPath), $"Verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("Expected at least 10 ISO52016 Matrix external validation anchor fixtures", script);
        Assert.Contains("manifest is missing fixture listed on disk", script);
        Assert.Contains("Duplicate ISO52016 Matrix external validation anchor id", script);
        Assert.Contains("ValidationAnchorOnly", script);
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