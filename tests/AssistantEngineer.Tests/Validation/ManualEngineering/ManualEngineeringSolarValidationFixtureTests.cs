using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.ManualEngineering;

public sealed class ManualEngineeringSolarValidationFixtureTests
{
    [Fact]
    public void FixtureFilesExist()
    {
        var requiredFiles = new[]
        {
            CaseMetadataPath,
            DerivationPath,
            InputPath,
            ExpectedOutputPath,
            TolerancesPath,
            ManualIndexPath
        };

        foreach (var file in requiredFiles)
            Assert.True(File.Exists(file), $"Required manual fixture file is missing: {file}");
    }

    [Fact]
    public void MetadataContainsCaseIdTierDomainAndRequiredNonClaims()
    {
        using var metadata = ReadJson(CaseMetadataPath);
        var root = metadata.RootElement;

        Assert.Equal("MAN-ENG-SOLAR-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Tier1ManualEngineering", root.GetProperty("tier").GetString());
        Assert.Equal("SolarGains", root.GetProperty("domain").GetString());
        Assert.Equal("Active", root.GetProperty("status").GetString());
        Assert.Equal("Independent manual derivation", root.GetProperty("source").GetString());
        Assert.Equal("2026-05-14", root.GetProperty("createdDate").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());

        var nonClaims = root
            .GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        var py = "py";
        var buildingEnergy = "BuildingEnergy";
        var expectedPyParityNonClaim = $"No {py}{buildingEnergy} parity claim";

        Assert.Contains("No ASHRAE 140 compliance claim", nonClaims);
        Assert.Contains("No exact EnergyPlus equivalence claim", nonClaims);
        Assert.Contains(expectedPyParityNonClaim, nonClaims);
        Assert.Contains("No full ISO/EN compliance claim", nonClaims);
        Assert.Contains("No Perez anisotropic model validation claim", nonClaims);
        Assert.Contains("No ISO 52016 full validation claim", nonClaims);
    }

    [Fact]
    public void InputContainsExplicitExclusions()
    {
        using var input = ReadJson(InputPath);
        var exclusions = input.RootElement.GetProperty("exclusions");

        Assert.True(exclusions.GetProperty("solarPositionCalculationExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("perezAnisotropicModelExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("diffuseDirectSplitExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("dynamicGlazingExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("thermalMassExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("internalGainsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("transmissionLossExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("hvacResponseExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("weatherFileExcluded").GetBoolean());
    }

    [Fact]
    public void ExpectedOutputMatchesIndependentManualFormulaWithinTolerance()
    {
        using var input = ReadJson(InputPath);
        using var expected = ReadJson(ExpectedOutputPath);
        using var tolerances = ReadJson(TolerancesPath);

        var inputRoot = input.RootElement;
        var expectedRoot = expected.RootElement;

        var area = inputRoot.GetProperty("windowAreaM2").GetDouble();
        var irradiance = inputRoot.GetProperty("incidentSurfaceIrradianceWPerM2").GetDouble();
        var shgc = inputRoot.GetProperty("solarHeatGainCoefficient").GetDouble();
        var shadingFactor = inputRoot.GetProperty("shadingFactor").GetDouble();

        var unshaded = area * irradiance * shgc;
        var net = unshaded * shadingFactor;
        var reduction = unshaded - net;
        var netKw = net / 1000.0;

        var absoluteTolerance = tolerances.RootElement.GetProperty("absoluteToleranceW").GetDouble();
        var relativeTolerance = tolerances.RootElement.GetProperty("relativeTolerance").GetDouble();

        AssertClose(area, expectedRoot.GetProperty("windowAreaM2").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(irradiance, expectedRoot.GetProperty("incidentSurfaceIrradianceWPerM2").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(shgc, expectedRoot.GetProperty("solarHeatGainCoefficient").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(shadingFactor, expectedRoot.GetProperty("shadingFactor").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(unshaded, expectedRoot.GetProperty("unshadedSolarGainW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(reduction, expectedRoot.GetProperty("shadingReductionW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(net, expectedRoot.GetProperty("netSolarGainW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(netKw, expectedRoot.GetProperty("netSolarGainKw").GetDouble(), absoluteTolerance / 1000.0, relativeTolerance);
    }

    [Fact]
    public void DerivationContainsFormulaStepsExpectedValuesAndNonClaims()
    {
        var content = File.ReadAllText(DerivationPath);

        var requiredPhrases = new[]
        {
            "Q_solar_W",
            "4.0 * 500.0 * 0.50 * 0.80",
            "800.0 W",
            "No Perez anisotropic model validation claim",
            "No ISO 52016 full validation claim"
        };

        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualFixtureIndexContainsSolarCase()
    {
        var content = File.ReadAllText(ManualIndexPath);

        Assert.Contains("MAN-ENG-SOLAR-001", content, StringComparison.Ordinal);
        Assert.Contains("800.0 W", content, StringComparison.Ordinal);
        Assert.Contains("MAN-ENG-SOLAR-001-simple-window-solar-gain", content, StringComparison.Ordinal);
        Assert.Contains("solarPositionCalculationExcluded", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualFixtureDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                CaseMetadataPath,
                DerivationPath,
                ManualIndexPath,
                RoadmapPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static void AssertClose(double expected, double actual, double absoluteTolerance, double relativeTolerance)
    {
        var absDiff = Math.Abs(expected - actual);
        if (absDiff <= absoluteTolerance)
            return;

        var scale = Math.Max(Math.Abs(expected), Math.Abs(actual));
        if (scale <= double.Epsilon)
            return;

        var relDiff = absDiff / scale;
        Assert.True(
            relDiff <= relativeTolerance,
            $"Values are not within tolerance. Expected={expected}, Actual={actual}, AbsDiff={absDiff}, RelDiff={relDiff}.");
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string FixtureDirectory =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "validation",
            "manual",
            "MAN-ENG-SOLAR-001-simple-window-solar-gain");

    private static string CaseMetadataPath => Path.Combine(FixtureDirectory, "case-metadata.json");
    private static string DerivationPath => Path.Combine(FixtureDirectory, "derivation.md");
    private static string InputPath => Path.Combine(FixtureDirectory, "input.json");
    private static string ExpectedOutputPath => Path.Combine(FixtureDirectory, "expected-output.json");
    private static string TolerancesPath => Path.Combine(FixtureDirectory, "comparison-tolerances.json");

    private static string ManualIndexPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "manual-engineering-fixtures.md");

    private static string RoadmapPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "external-numerical-validation-roadmap.md");
}
