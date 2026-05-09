using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Verification;

public sealed class Iso52016MultiZoneStageVerificationTests
{
    private static readonly string[] ExpectedFixtureFiles =
    {
        "two-zone-independent.json",
        "two-zone-interzone-conductance.json",
        "adjacent-unconditioned-zone.json",
        "same-use-adiabatic-boundary.json"
    };

    [Fact]
    public void MultiZoneCalculationDoc_DefinesClaimBoundaryAsInternalAnchor()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "Iso52016MultiZoneCalculation.md");

        Assert.True(File.Exists(docPath), $"Multi-zone calculation doc was not found at '{docPath}'.");

        var text = File.ReadAllText(docPath);

        Assert.Contains("standard-based multi-zone calculation", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("internal engineering anchor", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation anchor", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not full validation", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Supported scope", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unsupported scope", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultiZoneFixtureSet_IsPresent()
    {
        var fixtureDirectory = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "iso52016",
            "multi-zone");

        Assert.True(
            Directory.Exists(fixtureDirectory),
            $"Multi-zone fixture directory was not found at '{fixtureDirectory}'.");

        foreach (var expectedFile in ExpectedFixtureFiles)
        {
            var fullPath = Path.Combine(fixtureDirectory, expectedFile);
            Assert.True(File.Exists(fullPath), $"Expected multi-zone fixture was not found: '{fullPath}'.");
        }
    }

    [Fact]
    public void MultiZoneCalculationDoc_ListsAllFixtureFiles()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "Iso52016MultiZoneCalculation.md");
        var text = File.ReadAllText(docPath);

        foreach (var expectedFile in ExpectedFixtureFiles)
        {
            Assert.Contains(expectedFile, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MultiZoneStageDocs_DoNotContainUnsupportedPositiveClaims()
    {
        var files = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "Iso52016MultiZoneCalculation.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "EngineeringCoreV2Scope.md")
        };

        var forbiddenPositiveClaims = new[]
        {
            "full ISO52016 compliance",
            "external validation",
            "full airflow network",
            "moisture/latent coupling",
            "detailed HVAC plant coupling"
        };

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);

            foreach (var line in lines)
            {
                foreach (var claim in forbiddenPositiveClaims)
                {
                    if (!line.Contains(claim, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Assert.True(
                        line.Contains("no ", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("not ", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("does not", StringComparison.OrdinalIgnoreCase),
                        $"Unsupported positive claim found in '{file}': {line}");
                }
            }
        }
    }

    [Fact]
    public void MultiZoneSourceAndFixtures_UseStandardBasedCalculationLabeling()
    {
        var sourcePath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "MultiZone",
            "Iso52016MultiZoneHourlySolver.cs");
        var sourceText = File.ReadAllText(sourcePath);
        var legacyProductLabel = "Energy Calculation " + "equivalence";

        Assert.Contains("Standard-based multi-zone calculation completed", sourceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(legacyProductLabel, sourceText, StringComparison.OrdinalIgnoreCase);

        var fixtureDirectory = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "iso52016",
            "multi-zone");

        foreach (var fileName in ExpectedFixtureFiles)
        {
            var filePath = Path.Combine(fixtureDirectory, fileName);
            using var document = JsonDocument.Parse(File.ReadAllText(filePath));

            var claimFlags = document
                .RootElement
                .GetProperty("input")
                .GetProperty("claimFlags")
                .EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => item is not null)
                .ToArray();

            Assert.Contains("standard-based calculation", claimFlags, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MultiZoneStage_IsRegisteredInVerificationRegistryAndManifest()
    {
        RegistryContainsStageFile(
            "ISO52016-MULTI-ZONE-CALCULATION",
            "relatedManifests",
            "docs/releases/Iso52016MultiZoneCalculationStageManifest.json");
        RegistryContainsStageFile(
            "ISO52016-MULTI-ZONE-CALCULATION",
            "requiredDocs",
            "docs/calculations/Iso52016MultiZoneCalculation.md");
        RegistryContainsStageFile(
            "ISO52016-MULTI-ZONE-CALCULATION",
            "requiredDocs",
            "docs/calculations/EngineeringCoreV2Scope.md");
        RegistryContainsStageFile(
            "ISO52016-MULTI-ZONE-CALCULATION",
            "requiredTestFiles",
            "tests/fixtures/iso52016/multi-zone/two-zone-independent.json");
        RegistryContainsTestFilter(
            "ISO52016-MULTI-ZONE-CALCULATION",
            "FullyQualifiedName~Iso52016MultiZone");
    }
}
