using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixExternalValidationAnchorManifestTests
{
    [Fact]
    public void ExternalValidationAnchorManifest_ListsPatchScopeFixturesAndTheyExist()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnchorsManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("InProgress", root.GetProperty("status").GetString());
        Assert.Equal("AE-ISO52016-ANCHORS-001", root.GetProperty("patchId").GetString());
        Assert.True(root.GetProperty("plannedFinalFixtureCountIsNotRequiredInThisPatch").GetBoolean());

        var manifestFixtures = root
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => item is not null)
            .Cast<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(3, root.GetProperty("minimumFixtureCountForPatch").GetInt32());
        Assert.Equal(10, root.GetProperty("plannedFinalFixtureCount").GetInt32());
        Assert.True(manifestFixtures.Length >= root.GetProperty("minimumFixtureCountForPatch").GetInt32());

        foreach (var fixtureRelativePath in manifestFixtures)
        {
            Assert.StartsWith(
                "tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/ExternalValidationAnchors/manual-iso52016-anchor-",
                fixtureRelativePath,
                StringComparison.OrdinalIgnoreCase);

            Assert.True(
                File.Exists(Path.Combine(fixtureRelativePath.Split('/').Prepend(repoRoot).ToArray())),
                $"Manifest references missing anchor fixture: {fixtureRelativePath}");
        }
    }

    [Fact]
    public void ExternalValidationAnchorFixtures_HaveUniqueIdsAndValidationAnchorOnlyScope()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = ManifestFixtureFiles().ToArray();

        Assert.True(files.Length >= 3, $"Expected at least 3 patch-scope manual anchor fixtures, found {files.Length}.");

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var root = document.RootElement;

            var anchorId = root.GetProperty("anchorId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(anchorId));
            Assert.True(ids.Add(anchorId), $"Duplicate external validation anchor id: {anchorId}");

            Assert.Equal("ManualEngineeringValidationAnchor", root.GetProperty("sourceType").GetString());
            Assert.Equal("IndependentManualEngineeringFormula", root.GetProperty("authoritativeReference").GetString());
            Assert.Equal("Validation anchor only; not full parity.", root.GetProperty("validationClaim").GetString());

            var serializedFixture = root.GetRawText();
            Assert.DoesNotContain("pyBuildingEnergy output is authoritative", serializedFixture, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EnergyPlus output is authoritative", serializedFixture, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("claims full parity", serializedFixture, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ExternalValidationAnchorVerificationScript_GuardsManifestCompletenessAndPatchFixtureCount()
    {
        var repoRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-external-validation-anchors.ps1");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("Iso52016MatrixExternalValidationAnchorsManifest.json", script);
        Assert.Contains("$manifestFixtureFiles", script);
        Assert.Contains("Expected at least 3 ISO52016 Matrix external validation anchor fixtures", script);
        Assert.DoesNotContain("Expected at least 10 ISO52016 Matrix external validation anchor fixtures", script);
        Assert.DoesNotContain("does not match files on disk", script);
        Assert.Contains("ManualEngineeringValidationAnchor", script);
        Assert.Contains("IndependentManualEngineeringFormula", script);
        Assert.Contains("Validation anchor only; not full parity.", script);
    }

    private static IEnumerable<string> ManifestFixtureFiles()
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixExternalValidationAnchorsManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));

        return document.RootElement
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => Path.Combine(item!.Split('/').Prepend(repoRoot).ToArray()))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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