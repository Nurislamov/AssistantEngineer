using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.ManualEngineering;

public sealed class ManualEngineeringDomesticHotWaterValidationFixtureTests
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

        Assert.Equal("MAN-ENG-DHW-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Tier1ManualEngineering", root.GetProperty("tier").GetString());
        Assert.Equal("DomesticHotWater", root.GetProperty("domain").GetString());
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
        Assert.Contains("No ISO 12831-3 full validation claim", nonClaims);
        Assert.Contains("No EN 15316 full system-energy validation claim", nonClaims);
    }

    [Fact]
    public void InputContainsExplicitExclusions()
    {
        using var input = ReadJson(InputPath);
        var exclusions = input.RootElement.GetProperty("exclusions");

        Assert.True(exclusions.GetProperty("distributionLossesExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("storageLossesExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("circulationLossesExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("systemEfficiencyExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("heatRecoveryExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("monthlyProfileExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("annualProfileExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("peakSizingExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("en15316SystemChainExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("iso12831_3DetailedMethodExcluded").GetBoolean());
    }

    [Fact]
    public void ExpectedOutputMatchesIndependentManualFormulaWithinTolerance()
    {
        using var input = ReadJson(InputPath);
        using var expected = ReadJson(ExpectedOutputPath);
        using var tolerances = ReadJson(TolerancesPath);

        var inputRoot = input.RootElement;
        var expectedRoot = expected.RootElement;

        var dailyVolumeL = inputRoot.GetProperty("dailyHotWaterVolumeL").GetDouble();
        var density = inputRoot.GetProperty("waterDensityKgPerL").GetDouble();
        var coldTemp = inputRoot.GetProperty("coldWaterTemperatureC").GetDouble();
        var hotTemp = inputRoot.GetProperty("hotWaterTemperatureC").GetDouble();
        var specificHeat = inputRoot.GetProperty("specificHeatCapacityKWhPerKgK").GetDouble();

        var massKgPerDay = dailyVolumeL * density;
        var deltaT = hotTemp - coldTemp;
        var dailyKWh = massKgPerDay * specificHeat * deltaT;
        var dailyWh = dailyKWh * 1000.0;
        var averagePowerW = dailyWh / 24.0;

        var absTolKWh = tolerances.RootElement.GetProperty("absoluteToleranceKWh").GetDouble();
        var absTolWh = tolerances.RootElement.GetProperty("absoluteToleranceWh").GetDouble();
        var absTolW = tolerances.RootElement.GetProperty("absoluteToleranceW").GetDouble();
        var relTol = tolerances.RootElement.GetProperty("relativeTolerance").GetDouble();

        AssertClose(dailyVolumeL, expectedRoot.GetProperty("dailyHotWaterVolumeL").GetDouble(), absTolWh, relTol);
        AssertClose(density, expectedRoot.GetProperty("waterDensityKgPerL").GetDouble(), absTolWh, relTol);
        AssertClose(massKgPerDay, expectedRoot.GetProperty("waterMassKgPerDay").GetDouble(), absTolWh, relTol);
        AssertClose(coldTemp, expectedRoot.GetProperty("coldWaterTemperatureC").GetDouble(), absTolWh, relTol);
        AssertClose(hotTemp, expectedRoot.GetProperty("hotWaterTemperatureC").GetDouble(), absTolWh, relTol);
        AssertClose(deltaT, expectedRoot.GetProperty("deltaTemperatureK").GetDouble(), absTolWh, relTol);
        AssertClose(specificHeat, expectedRoot.GetProperty("specificHeatCapacityKWhPerKgK").GetDouble(), absTolKWh, relTol);
        AssertClose(dailyKWh, expectedRoot.GetProperty("dailyUsefulDhwEnergyKWh").GetDouble(), absTolKWh, relTol);
        AssertClose(dailyWh, expectedRoot.GetProperty("dailyUsefulDhwEnergyWh").GetDouble(), absTolWh, relTol);
        AssertClose(averagePowerW, expectedRoot.GetProperty("averageDailyUsefulDhwPowerW").GetDouble(), absTolW, relTol);
    }

    [Fact]
    public void DerivationContainsFormulaExpectedValuesAndNonClaims()
    {
        var content = File.ReadAllText(DerivationPath);

        var requiredPhrases = new[]
        {
            "Q_DHW_kWh",
            "200.0 * 0.001163 * 45.0",
            "10.467 kWh/day",
            "10467.0 Wh/day",
            "436.125 W",
            "No ISO 12831-3 full validation claim",
            "No EN 15316 full system-energy validation claim"
        };

        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualFixtureIndexContainsDhwCase()
    {
        var content = File.ReadAllText(ManualIndexPath);

        Assert.Contains("MAN-ENG-DHW-001", content, StringComparison.Ordinal);
        Assert.Contains("10.467 kWh/day", content, StringComparison.Ordinal);
        Assert.Contains("436.125 W", content, StringComparison.Ordinal);
        Assert.Contains("MAN-ENG-DHW-001-simple-domestic-hot-water-demand", content, StringComparison.Ordinal);
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
            "MAN-ENG-DHW-001-simple-domestic-hot-water-demand");

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
