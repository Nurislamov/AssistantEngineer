using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixExternalValidationAnchorManifestTests
{
    private static readonly string[] RequiredAnchorIds =
    [
        "MANUAL-ISO52016-ANCHOR-001",
        "MANUAL-ISO52016-ANCHOR-002",
        "MANUAL-ISO52016-ANCHOR-003",
        "MANUAL-ISO52016-ANCHOR-004",
        "MANUAL-ISO52016-ANNUAL-8760-001"
    ];

    [Fact]
    public void ExternalValidationAnchorManifest_ListsEveryRequiredAnchorAndFixture()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var root = document.RootElement;

        Assert.Equal("ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS", root.GetProperty("stageId").GetString());
        Assert.Equal("ValidationAnchorsOnly", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("stageComplete").GetBoolean());

        var requiredAnchorIds = root
            .GetProperty("requiredAnchorIds")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        var fixtures = root
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal(RequiredAnchorIds.Length, root.GetProperty("fixtureCount").GetInt32());
        Assert.Equal(RequiredAnchorIds.Length, fixtures.Length);

        foreach (var requiredAnchorId in RequiredAnchorIds)
        {
            Assert.Contains(requiredAnchorId, requiredAnchorIds);
        }
    }

    [Fact]
    public void ExternalValidationAnchorManifest_FixtureFilesExistAndUseIndependentManualScope()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var fixtures = document.RootElement
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        var anchorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in fixtures)
        {
            var fixturePath = Path.Combine(relativePath!.Split('/').Prepend(RepoRoot).ToArray());

            Assert.True(File.Exists(fixturePath), $"External validation anchor fixture was not found: {fixturePath}");

            using var fixtureDocument = JsonDocument.Parse(File.ReadAllText(fixturePath));
            var fixture = fixtureDocument.RootElement;

            Assert.True(anchorIds.Add(fixture.GetProperty("anchorId").GetString()!));
            Assert.Equal("ManualEngineeringValidationAnchor", fixture.GetProperty("sourceType").GetString());
            Assert.Equal("IndependentManualEngineeringFormula", fixture.GetProperty("authoritativeReference").GetString());
            Assert.Equal("ValidationAnchorsOnly", fixture.GetProperty("scope").GetString());

            var nonClaims = fixture
                .GetProperty("explicitNonClaims")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();

            Assert.Contains("No StandardReference equivalence claim.", nonClaims);
            Assert.Contains("No EnergyPlus comparison workflow claim.", nonClaims);
            Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor coverage claim.", nonClaims);
            Assert.Contains("No full ISO 52016 equivalence claim.", nonClaims);
        }

        foreach (var requiredAnchorId in RequiredAnchorIds)
        {
            Assert.Contains(requiredAnchorId, anchorIds);
        }
    }

    [Fact]
    public void Annual8760Fixture_IsExplicitConstantWeatherAnnualReference()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var annualFixturePath = document.RootElement
            .GetProperty("fixtures")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Single(path => path is not null && path.Contains("annual-8760", StringComparison.OrdinalIgnoreCase));

        var fixturePath = Path.Combine(annualFixturePath!.Split('/').Prepend(RepoRoot).ToArray());
        using var fixtureDocument = JsonDocument.Parse(File.ReadAllText(fixturePath));
        var fixture = fixtureDocument.RootElement;

        Assert.Equal("MANUAL-ISO52016-ANNUAL-8760-001", fixture.GetProperty("anchorId").GetString());
        Assert.Equal(8760, fixture.GetProperty("hourCount").GetInt32());
        Assert.Equal("AnnualConstantHeating", fixture.GetProperty("mode").GetString());

        var expected = fixture.GetProperty("expected");

        Assert.Equal(275.0, expected.GetProperty("heatingLoadW").GetDouble());
        Assert.Equal(2409.0, expected.GetProperty("annualHeatingEnergyKWh").GetDouble());
    }

    [Fact]
    public void ExternalValidationAnchorDocumentation_StatesNonClaimsAndSourcePolicy()
    {
        var docPath = Path.Combine(
            RepoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixExternalValidationAnchors.md");

        Assert.True(File.Exists(docPath), $"External validation anchors doc was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("validation anchors only, not full equivalence claim", doc);
        Assert.Contains("IndependentManualEngineeringFormula", doc);
        Assert.Contains("No StandardReference equivalence claim.", doc);
        Assert.Contains("No EnergyPlus comparison workflow claim.", doc);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor coverage claim.", doc);
        Assert.Contains("No full ISO 52016 equivalence claim.", doc);
        Assert.Contains("MANUAL-ISO52016-ANNUAL-8760-001", doc);
    }

    [Fact]
    public void ExternalValidationAnchorVerificationScript_GuardsManifestCompletenessAndNonClaims()
    {
        var scriptPath = Path.Combine(
            RepoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-external-validation-anchors.ps1");

        Assert.True(File.Exists(scriptPath), $"External validation anchors verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        foreach (var requiredAnchorId in RequiredAnchorIds)
        {
            Assert.Contains(requiredAnchorId, script);
        }

        Assert.Contains("ManualEngineeringValidationAnchor", script);
        Assert.Contains("IndependentManualEngineeringFormula", script);
        Assert.Contains("ValidationAnchorsOnly", script);
        Assert.Contains("No StandardReference equivalence claim.", script);
        Assert.Contains("No EnergyPlus comparison workflow claim.", script);
        Assert.Contains("hourCount 8760", script);
    }

    [Fact]
    public void VerificationRegistry_ReferencesExternalValidationAnchorGate()
    {
        RegistryContainsStageFile(
            "ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS",
            "relatedManifests",
            "docs/releases/Iso52016MatrixExternalValidationAnchorsManifest.json");
        RegistryContainsTestFilter(
            "ISO52016-MATRIX-EXTERNAL-VALIDATION-ANCHORS",
            "FullyQualifiedName~Iso52016MatrixExternalValidationAnchor");
    }

    private static string RepoRoot => FindRepositoryRoot();

    private static string ManifestPath => Path.Combine(
        RepoRoot,
        "docs",
        "releases",
        "Iso52016MatrixExternalValidationAnchorsManifest.json");

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
