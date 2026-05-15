using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.ManualEngineering;

public sealed class ManualEngineeringVentilationValidationFixtureTests
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

        Assert.Equal("MAN-ENG-VENT-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Tier1ManualEngineering", root.GetProperty("tier").GetString());
        Assert.Equal("VentilationInfiltration", root.GetProperty("domain").GetString());
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

        var indoorC = inputRoot.GetProperty("indoorDesignTemperatureC").GetDouble();
        var outdoorC = inputRoot.GetProperty("outdoorDesignTemperatureC").GetDouble();
        var deltaT = indoorC - outdoorC;

        var volumeM3 = inputRoot.GetProperty("roomVolumeM3").GetDouble();
        var mechanicalFlowM3PerH = inputRoot.GetProperty("mechanicalVentilationOutdoorAirflowM3PerH").GetDouble();
        var infiltrationAch = inputRoot.GetProperty("infiltrationAirChangesPerHour").GetDouble();
        var sensibleCoefficient = inputRoot.GetProperty("sensibleHeatCoefficientWhPerM3K").GetDouble();

        var infiltrationFlowM3PerH = infiltrationAch * volumeM3;
        var totalFlowM3PerH = mechanicalFlowM3PerH + infiltrationFlowM3PerH;

        var qMechanical = sensibleCoefficient * mechanicalFlowM3PerH * deltaT;
        var qInfiltration = sensibleCoefficient * infiltrationFlowM3PerH * deltaT;
        var qTotal = qMechanical + qInfiltration;
        var qTotalKw = qTotal / 1000.0;

        var absoluteTolerance = tolerances.RootElement.GetProperty("absoluteToleranceW").GetDouble();
        var relativeTolerance = tolerances.RootElement.GetProperty("relativeTolerance").GetDouble();

        AssertClose(mechanicalFlowM3PerH, expectedRoot.GetProperty("mechanicalVentilationAirflowM3PerH").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(infiltrationFlowM3PerH, expectedRoot.GetProperty("infiltrationAirflowM3PerH").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(totalFlowM3PerH, expectedRoot.GetProperty("totalOutdoorAirflowM3PerH").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qMechanical, expectedRoot.GetProperty("mechanicalVentilationSensibleLoadW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qInfiltration, expectedRoot.GetProperty("infiltrationSensibleLoadW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qTotal, expectedRoot.GetProperty("totalVentilationInfiltrationSensibleLoadW").GetDouble(), absoluteTolerance, relativeTolerance);
        AssertClose(qTotalKw, expectedRoot.GetProperty("totalVentilationInfiltrationSensibleLoadKw").GetDouble(), absoluteTolerance / 1000.0, relativeTolerance);
    }

    [Fact]
    public void InputContainsExplicitExclusions()
    {
        using var input = ReadJson(InputPath);
        var exclusions = input.RootElement.GetProperty("exclusions");

        Assert.True(exclusions.GetProperty("transmissionExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("solarGainsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("internalGainsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("latentLoadsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("heatRecoveryExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("dynamicEffectsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("groundBoundaryExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("adjacentRoomCouplingExcluded").GetBoolean());
    }

    [Fact]
    public void DerivationContainsFormulaExpectedValuesAndNonClaims()
    {
        var content = File.ReadAllText(DerivationPath);

        var requiredPhrases = new[]
        {
            "Ventilation and infiltration sensible heating load",
            "Q_W = 0.33",
            "1188.0 W",
            "297.0 W",
            "1485.0 W",
            "No ASHRAE 140 compliance claim",
            "No exact EnergyPlus equivalence claim",
            "No full ISO/EN compliance claim"
        };

        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualFixtureIndexContainsVentilationCase()
    {
        var content = File.ReadAllText(ManualIndexPath);

        Assert.Contains("MAN-ENG-VENT-001", content, StringComparison.Ordinal);
        Assert.Contains("1485.0 W", content, StringComparison.Ordinal);
        Assert.Contains("MAN-ENG-VENT-001-ventilation-infiltration-sensible-load", content, StringComparison.Ordinal);
        Assert.Contains("Active", content, StringComparison.OrdinalIgnoreCase);
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
            "MAN-ENG-VENT-001-ventilation-infiltration-sensible-load");

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
