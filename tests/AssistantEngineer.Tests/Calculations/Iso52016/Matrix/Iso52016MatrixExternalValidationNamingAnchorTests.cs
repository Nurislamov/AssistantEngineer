using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixExternalValidationNamingAnchorTests
{
    [Fact]
    public void NamingAnchorFixtures_AreValidationAnchorsOnlyAndCoverExpectedStyleFamilies()
    {
        var repoRoot = FindRepositoryRoot();
        var fixtureDirectory = Path.Combine(
            repoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Calculations",
            "Iso52016",
            "Matrix",
            "ExternalValidationNamingAnchors");

        Assert.True(
            Directory.Exists(fixtureDirectory),
            $"ISO52016 Matrix naming anchor fixture directory was not found: {fixtureDirectory}");

        var fixtureFiles = Directory
            .GetFiles(fixtureDirectory, "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            fixtureFiles.Length >= 4,
            $"Expected at least 4 ISO52016 Matrix naming anchor fixtures, found {fixtureFiles.Length}.");

        var anchorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var styleFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fixtureFile in fixtureFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fixtureFile));
            var root = document.RootElement;

            var anchorId = root.GetProperty("anchorId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(anchorId));
            Assert.True(anchorIds.Add(anchorId!), $"Duplicate naming anchor id: {anchorId}");

            Assert.Equal("ValidationAnchorOnly", root.GetProperty("scope").GetString());

            var sourceStyle = root.GetProperty("sourceStyle").GetString();
            Assert.False(string.IsNullOrWhiteSpace(sourceStyle));
            styleFamilies.Add(sourceStyle!);

            Assert.Equal("ExternalStyleNameTraceabilityOnly", root.GetProperty("namingPurpose").GetString());

            var expected = root.GetProperty("expected");
            Assert.True(expected.TryGetProperty("heatingLoadW", out _));
            Assert.True(expected.TryGetProperty("coolingLoadW", out _));
            Assert.True(expected.TryGetProperty("heatingEnergyKWh", out _));
            Assert.True(expected.TryGetProperty("coolingEnergyKWh", out _));

            var nonClaims = root
                .GetProperty("explicitNonClaims")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();

            Assert.Contains("Validation anchors only, not full parity.", nonClaims);
            Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", nonClaims);
            Assert.Contains("No exact EnergyPlus numerical parity claim.", nonClaims);
            Assert.Contains("No ASHRAE 140 validation coverage claim.", nonClaims);
        }

        Assert.Contains("PyBuildingEnergyStyleNamesOnly", styleFamilies);
        Assert.Contains("EnergyPlusStyleNamesOnly", styleFamilies);
    }

    [Fact]
    public void NamingAnchorDocsManifestAndVerificationScript_DoNotClaimFullParity()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixExternalValidationNamingAnchors.md");

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationNamingAnchorsManifest.json");

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-external-validation-naming-anchors.ps1");

        Assert.True(File.Exists(docPath), $"Naming anchors doc was not found: {docPath}");
        Assert.True(File.Exists(manifestPath), $"Naming anchors manifest was not found: {manifestPath}");
        Assert.True(File.Exists(scriptPath), $"Naming anchors verification script was not found: {scriptPath}");

        var doc = File.ReadAllText(docPath);
        var manifestText = File.ReadAllText(manifestPath);
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("Validation anchors only, not full parity.", doc);
        Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", doc);
        Assert.Contains("No exact EnergyPlus numerical parity claim.", doc);
        Assert.Contains("No ExternalParityCovered claim.", doc);

        Assert.Contains("verify-iso52016-matrix-external-validation-naming-anchors.ps1", script);
        Assert.Contains("ValidationAnchorOnly", script);

        Assert.DoesNotContain("\"ExternalParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"FullParityCovered\": true", manifestText, StringComparison.OrdinalIgnoreCase);

        using var manifest = JsonDocument.Parse(manifestText);
        var root = manifest.RootElement;

        Assert.Equal("ISO52016-MATRIX-EXTERNAL-VALIDATION-NAMING-ANCHORS", root.GetProperty("stageId").GetString());
        Assert.Equal("ValidationAnchorOnly", root.GetProperty("scope").GetString());
        Assert.True(root.GetProperty("namingAnchorsIntegrated").GetBoolean());
        Assert.True(root.GetProperty("pyBuildingEnergyStyleNamingIntegrated").GetBoolean());
        Assert.True(root.GetProperty("energyPlusStyleNamingIntegrated").GetBoolean());

        var nonClaims = root
            .GetProperty("explicitNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Validation anchors only, not full parity.", nonClaims);
        Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", nonClaims);
        Assert.Contains("No exact EnergyPlus numerical parity claim.", nonClaims);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", nonClaims);
    }

    [Fact]
    public void NamingAnchorManifest_MatchesFixtureFilesOnDisk()
    {
        var repoRoot = FindRepositoryRoot();

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationNamingAnchorsManifest.json");

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));

        var fixtureFiles = manifest.RootElement
            .GetProperty("fixtureFiles")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(fixtureFiles.Length >= 4);

        foreach (var relativePath in fixtureFiles)
        {
            var fixturePath = Path.Combine(relativePath.Split('/').Prepend(repoRoot).ToArray());
            Assert.True(File.Exists(fixturePath), $"Manifest fixture file was not found: {relativePath}");
        }

        var diskFixtureFiles = Directory
            .GetFiles(
                Path.Combine(
                    repoRoot,
                    "tests",
                    "AssistantEngineer.Tests",
                    "Calculations",
                    "Iso52016",
                    "Matrix",
                    "ExternalValidationNamingAnchors"),
                "*.json")
            .Select(path => path.Replace(repoRoot + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(diskFixtureFiles, fixtureFiles);
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