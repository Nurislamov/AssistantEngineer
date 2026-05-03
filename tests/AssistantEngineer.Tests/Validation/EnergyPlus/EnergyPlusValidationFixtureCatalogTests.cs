using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationFixtureCatalogTests
{
    [Fact]
    public void FixtureCatalogScriptGeneratedFilesAndGuideExist()
    {
        var requiredFiles = new[]
        {
            CatalogScriptPath,
            CatalogJsonPath,
            CatalogMarkdownPath,
            CatalogGuidePath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required EnergyPlus fixture catalog artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void CatalogDeclaresPlannedValidationAndSourcePaths()
    {
        using var document = ReadJson(CatalogJsonPath);
        var root = document.RootElement;

        Assert.Equal("EnergyPlus Validation Fixture Catalog", root.GetProperty("catalogName").GetString());
        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("PlannedValidation", root.GetProperty("status").GetString());

        Assert.Equal(
            "docs/validation/EnergyPlusValidationCaseRegistry.json",
            root.GetProperty("registryPath").GetString());

        Assert.Equal(
            "tests/fixtures/validation/energyplus",
            root.GetProperty("fixturesRoot").GetString());

        Assert.Equal(
            "docs/reports/validation",
            root.GetProperty("reportsDirectory").GetString());
    }

    [Fact]
    public void CatalogContainsSmoke001002003FixturesWithComparisonOutputs()
    {
        using var document = ReadJson(CatalogJsonPath);

        var fixtures = document
            .RootElement
            .GetProperty("fixtures")
            .EnumerateArray()
            .ToArray();

        var fixtureIds = fixtures
            .Select(item => item.GetProperty("caseId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("EP-SMOKE-001", fixtureIds);
        Assert.Contains("EP-SMOKE-002", fixtureIds);
        Assert.Contains("EP-SMOKE-003", fixtureIds);

        foreach (var caseId in new[] { "EP-SMOKE-001", "EP-SMOKE-002", "EP-SMOKE-003" })
        {
            var fixture = fixtures.Single(item => item.GetProperty("caseId").GetString() == caseId);

            Assert.True(fixture.GetProperty("registryListed").GetBoolean());
            Assert.True(fixture.GetProperty("hasMetadata").GetBoolean());
            Assert.True(fixture.GetProperty("hasAssistantEngineerInput").GetBoolean());
            Assert.True(fixture.GetProperty("hasComparisonTolerances").GetBoolean());
            Assert.True(fixture.GetProperty("hasPlaceholderReference").GetBoolean());
            Assert.True(fixture.GetProperty("hasFixtureReadme").GetBoolean());
            Assert.True(fixture.GetProperty("hasComparisonJson").GetBoolean());
            Assert.True(fixture.GetProperty("hasComparisonMarkdown").GetBoolean());
            Assert.Equal("PlaceholderComparison", fixture.GetProperty("comparisonStatus").GetString());
            Assert.Equal("PlaceholderReferenceOutput", fixture.GetProperty("referenceStatus").GetString());
            Assert.True(fixture.GetProperty("allMetricsPassed").GetBoolean());
            Assert.True(fixture.GetProperty("metricCount").GetInt32() >= 3);
        }
    }

    [Fact]
    public void CatalogSyncHasNoMissingSmokeFixtureFoldersNoUnknownFixturesAndNoMissingComparisonOutputs()
    {
        using var document = ReadJson(CatalogJsonPath);
        var sync = document.RootElement.GetProperty("sync");

        Assert.Empty(sync.GetProperty("fixturesWithoutRegistry").EnumerateArray());
        Assert.Empty(sync.GetProperty("fixturesMissingRequiredFiles").EnumerateArray());
        Assert.Empty(sync.GetProperty("fixturesMissingComparison").EnumerateArray());

        var registryCasesWithoutFixture = sync
            .GetProperty("registryCasesWithoutFixture")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain("EP-SMOKE-001", registryCasesWithoutFixture);
        Assert.DoesNotContain("EP-SMOKE-002", registryCasesWithoutFixture);
        Assert.DoesNotContain("EP-SMOKE-003", registryCasesWithoutFixture);
    }

    [Fact]
    public void CatalogTotalsReflectAtLeastThreeSmokeFixturesAndPlaceholderComparisons()
    {
        using var document = ReadJson(CatalogJsonPath);
        var totals = document.RootElement.GetProperty("totals");

        Assert.True(totals.GetProperty("registrySmokeCases").GetInt32() >= 3);
        Assert.True(totals.GetProperty("fixtureDirectories").GetInt32() >= 3);
        Assert.True(totals.GetProperty("fixturesWithComparison").GetInt32() >= 3);
        Assert.True(totals.GetProperty("placeholderComparisons").GetInt32() >= 3);
        Assert.Equal(0, totals.GetProperty("fixturesWithoutRegistry").GetInt32());
        Assert.Equal(0, totals.GetProperty("fixturesMissingRequiredFiles").GetInt32());
        Assert.Equal(0, totals.GetProperty("fixturesMissingComparison").GetInt32());
    }

    [Fact]
    public void CatalogKeepsRequiredNonClaimsVisible()
    {
        using var document = ReadJson(CatalogJsonPath);

        var nonClaims = document
            .RootElement
            .GetProperty("requiredNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim exact EnergyPlus", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim ASHRAE 140", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim full ISO 52016", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("PlaceholderComparison is not real EnergyPlus validation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Future real validation must remain tolerance-based", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CatalogMarkdownDocumentsSyncSectionsAndNonClaims()
    {
        var content = File.ReadAllText(CatalogMarkdownPath);

        var requiredPhrases = new[]
        {
            "EnergyPlus Validation Fixture Catalog",
            "EP-SMOKE-001",
            "EP-SMOKE-002",
            "EP-SMOKE-003",
            "Registry cases without fixture",
            "Fixtures without registry entry",
            "Fixtures missing required files",
            "Fixtures missing comparison output",
            "PlaceholderComparison is not real EnergyPlus validation",
            "does not claim exact EnergyPlus numerical parity",
            "ASHRAE 140 validation coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CatalogGuideDocumentsPurposeCommandChecksRequiredFilesAndGuardTests()
    {
        var content = File.ReadAllText(CatalogGuidePath);

        var requiredPhrases = new[]
        {
            "EnergyPlus Validation Fixture Catalog",
            "generate-energyplus-validation-fixture-catalog.ps1",
            "registry cases without fixture folders",
            "fixture folders without registry entries",
            "fixtures missing required files",
            "fixtures missing comparison output",
            "case-metadata.json",
            "assistantengineer-input.json",
            "comparison-tolerances.json",
            "reference-output.placeholder.json or energyplus-output.reference.json",
            "EnergyPlusValidationFixtureCatalogTests"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CatalogScriptReadsRegistryFixturesAndComparisonOutputs()
    {
        var content = File.ReadAllText(CatalogScriptPath);

        Assert.Contains("EnergyPlusValidationCaseRegistry.json", content, StringComparison.Ordinal);
        Assert.Contains("tests/fixtures/validation/energyplus", content, StringComparison.Ordinal);
        Assert.Contains("ComparisonResult.json", content, StringComparison.Ordinal);
        Assert.Contains("EnergyPlusValidationFixtureCatalog.json", content, StringComparison.Ordinal);
        Assert.Contains("EnergyPlusValidationFixtureCatalog.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RegenerateArtifactsScriptRunsFixtureCatalogGenerator()
    {
        var content = File.ReadAllText(RegenerateArtifactsScriptPath);

        Assert.Contains("generate-energyplus-validation-fixture-catalog.ps1", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MainVerificationScriptIncludesFixtureCatalogTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains("EnergyPlusValidationFixtureCatalogTests", content, StringComparison.Ordinal);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string CatalogScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "generate-energyplus-validation-fixture-catalog.ps1");

    private static string CatalogJsonPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "EnergyPlusValidationFixtureCatalog.json");

    private static string CatalogMarkdownPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "EnergyPlusValidationFixtureCatalog.md");

    private static string CatalogGuidePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "EnergyPlusValidationFixtureCatalogGuide.md");

    private static string RegenerateArtifactsScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "regenerate-engineering-core-v1-artifacts.ps1");

    private static string MainVerificationScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "verify-engineering-core-v1.ps1");
}
