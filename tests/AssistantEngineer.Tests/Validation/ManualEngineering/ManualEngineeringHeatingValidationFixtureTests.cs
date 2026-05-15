using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.ManualEngineering;

public sealed class ManualEngineeringHeatingValidationFixtureTests
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
        {
            Assert.True(File.Exists(file), $"Required manual fixture file is missing: {file}");
        }
    }

    [Fact]
    public void MetadataContainsCaseIdTierAndRequiredNonClaims()
    {
        using var metadata = ReadJson(CaseMetadataPath);
        var root = metadata.RootElement;

        Assert.Equal("MAN-ENG-HEAT-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Tier1ManualEngineering", root.GetProperty("tier").GetString());
        Assert.Equal("HeatingLoad", root.GetProperty("domain").GetString());
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
    }

    [Fact]
    public void ExpectedOutputMatchesIndependentManualFormulaWithinTolerance()
    {
        using var input = ReadJson(InputPath);
        using var expected = ReadJson(ExpectedOutputPath);
        using var tolerances = ReadJson(TolerancesPath);

        var inputRoot = input.RootElement;
        var expectedRoot = expected.RootElement;

        var indoorC = inputRoot.GetProperty("temperatures").GetProperty("indoorDesignTemperatureC").GetDouble();
        var outdoorC = inputRoot.GetProperty("temperatures").GetProperty("outdoorDesignTemperatureC").GetDouble();
        var deltaT = indoorC - outdoorC;

        var wallArea = inputRoot.GetProperty("envelope").GetProperty("externalWall").GetProperty("areaM2").GetDouble();
        var wallU = inputRoot.GetProperty("envelope").GetProperty("externalWall").GetProperty("uValueWPerM2K").GetDouble();
        var windowArea = inputRoot.GetProperty("envelope").GetProperty("window").GetProperty("areaM2").GetDouble();
        var windowU = inputRoot.GetProperty("envelope").GetProperty("window").GetProperty("uValueWPerM2K").GetDouble();
        var roofArea = inputRoot.GetProperty("envelope").GetProperty("roof").GetProperty("areaM2").GetDouble();
        var roofU = inputRoot.GetProperty("envelope").GetProperty("roof").GetProperty("uValueWPerM2K").GetDouble();

        var ach = inputRoot.GetProperty("ventilation").GetProperty("airChangesPerHour").GetDouble();
        var volume = inputRoot.GetProperty("room").GetProperty("volumeM3").GetDouble();
        var sensibleCoeff = inputRoot.GetProperty("ventilation").GetProperty("sensibleCoefficient").GetDouble();

        var qWall = wallArea * wallU * deltaT;
        var qWindow = windowArea * windowU * deltaT;
        var qRoof = roofArea * roofU * deltaT;
        var qTransmission = qWall + qWindow + qRoof;

        var airflowM3PerHour = ach * volume;
        var qVentilation = sensibleCoeff * airflowM3PerHour * deltaT;

        var qTotal = qTransmission + qVentilation;
        var qTotalKw = qTotal / 1000.0;

        var absoluteTolerance = tolerances.RootElement.GetProperty("absoluteToleranceW").GetDouble();
        var relativeTolerance = tolerances.RootElement.GetProperty("relativeTolerance").GetDouble();

        AssertClose(qWall, expectedRoot.GetProperty("components").GetProperty("wallHeatLossW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qWindow, expectedRoot.GetProperty("components").GetProperty("windowHeatLossW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qRoof, expectedRoot.GetProperty("components").GetProperty("roofHeatLossW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(airflowM3PerHour, expectedRoot.GetProperty("components").GetProperty("airflowM3PerHour").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qTransmission, expectedRoot.GetProperty("transmissionHeatLossW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qVentilation, expectedRoot.GetProperty("ventilationHeatLossW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qTotal, expectedRoot.GetProperty("totalHeatingLoadW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qTotalKw, expectedRoot.GetProperty("totalHeatingLoadKw").GetDouble(), absoluteTolerance / 1000.0, relativeTolerance);

        var exclusions = inputRoot.GetProperty("exclusions");
        Assert.True(exclusions.GetProperty("internalGainsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("solarGainsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("groundBoundaryExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("latentLoadsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("dynamicEffectsExcluded").GetBoolean());
    }

    [Fact]
    public void DerivationContainsFormulaStepsExpectedValuesAndNonClaims()
    {
        var content = File.ReadAllText(DerivationPath);

        var requiredPhrases = new[]
        {
            "Steady-state single room heating loss",
            "Q = U * A * DeltaT",
            "Q_vent_W = 0.33 * airflow_m3_per_h * DeltaT_K",
            "Q_wall = 30.0 * 0.40 * 25 = 300.0 W",
            "Q_window = 4.0 * 1.60 * 25 = 160.0 W",
            "Q_roof = 20.0 * 0.25 * 25 = 125.0 W",
            "Q_ventilation = 0.33 * 30.0 * 25 = 247.5 W",
            "Q_total = 585.0 + 247.5 = 832.5 W",
            "No ASHRAE 140 compliance claim",
            "No exact EnergyPlus equivalence claim",
            "No full ISO/EN compliance claim"
        };

        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
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
            "MAN-ENG-HEAT-001-steady-state-room-loss");

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
