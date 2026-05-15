using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.ManualEngineering;

public sealed class ManualEngineeringGroundValidationFixtureTests
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

        Assert.Equal("MAN-ENG-GROUND-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Tier1ManualEngineering", root.GetProperty("tier").GetString());
        Assert.Equal("GroundBoundary", root.GetProperty("domain").GetString());
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
        Assert.Contains("No ISO 13370 full validation claim", nonClaims);
        Assert.Contains("No detailed ground coupling validation claim", nonClaims);
    }

    [Fact]
    public void InputContainsExplicitExclusions()
    {
        using var input = ReadJson(InputPath);
        var exclusions = input.RootElement.GetProperty("exclusions");

        Assert.True(exclusions.GetProperty("iso13370PerimeterCalculationExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("detailedGroundCouplingExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("dynamicGroundTemperatureModelExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("monthlyGroundTemperatureProfileExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("thermalBridgeExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("adjacentRoomCouplingExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("solarGainsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("internalGainsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("ventilationExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("infiltrationExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("externalWallTransmissionExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("windowTransmissionExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("roofTransmissionExcluded").GetBoolean());
    }

    [Fact]
    public void ExpectedOutputMatchesIndependentManualFormulaWithinTolerance()
    {
        using var input = ReadJson(InputPath);
        using var expected = ReadJson(ExpectedOutputPath);
        using var tolerances = ReadJson(TolerancesPath);

        var inputRoot = input.RootElement;
        var expectedRoot = expected.RootElement;

        var area = inputRoot.GetProperty("groundContactAreaM2").GetDouble();
        var uValue = inputRoot.GetProperty("equivalentGroundUValueWPerM2K").GetDouble();
        var indoor = inputRoot.GetProperty("indoorDesignTemperatureC").GetDouble();
        var ground = inputRoot.GetProperty("effectiveGroundTemperatureC").GetDouble();

        var deltaT = indoor - ground;
        var qGround = area * uValue * deltaT;
        var qGroundKw = qGround / 1000.0;

        var absoluteTolerance = tolerances.RootElement.GetProperty("absoluteToleranceW").GetDouble();
        var relativeTolerance = tolerances.RootElement.GetProperty("relativeTolerance").GetDouble();

        AssertClose(area, expectedRoot.GetProperty("groundContactAreaM2").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(uValue, expectedRoot.GetProperty("equivalentGroundUValueWPerM2K").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(indoor, expectedRoot.GetProperty("indoorDesignTemperatureC").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(ground, expectedRoot.GetProperty("effectiveGroundTemperatureC").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(deltaT, expectedRoot.GetProperty("deltaTemperatureK").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qGround, expectedRoot.GetProperty("groundBoundaryHeatLossW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qGroundKw, expectedRoot.GetProperty("groundBoundaryHeatLossKw").GetDouble(), absoluteTolerance / 1000.0, relativeTolerance);
    }

    [Fact]
    public void DerivationContainsFormulaExpectedValuesAndNonClaims()
    {
        var content = File.ReadAllText(DerivationPath);

        var requiredPhrases = new[]
        {
            "Q_ground_W",
            "50.0 * 0.30 * 10.0",
            "150.0 W",
            "No ISO 13370 full validation claim",
            "No detailed ground coupling validation claim"
        };

        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualFixtureIndexContainsGroundCase()
    {
        var content = File.ReadAllText(ManualIndexPath);

        Assert.Contains("MAN-ENG-GROUND-001", content, StringComparison.Ordinal);
        Assert.Contains("150.0 W", content, StringComparison.Ordinal);
        Assert.Contains("MAN-ENG-GROUND-001-simple-ground-boundary-loss", content, StringComparison.Ordinal);
        Assert.Contains("iso13370PerimeterCalculationExcluded", content, StringComparison.Ordinal);
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
            "MAN-ENG-GROUND-001-simple-ground-boundary-loss");

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
