using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.ManualEngineering;

public sealed class ManualEngineeringSystemEnergyValidationFixtureTests
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

        Assert.Equal("MAN-ENG-SYS-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Tier1ManualEngineering", root.GetProperty("tier").GetString());
        Assert.Equal("SystemEnergy", root.GetProperty("domain").GetString());
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
        Assert.Contains("No EN 15316 full validation claim", nonClaims);
        Assert.Contains("No detailed system-energy validation claim", nonClaims);
    }

    [Fact]
    public void InputContainsExplicitExclusions()
    {
        using var input = ReadJson(InputPath);
        var exclusions = input.RootElement.GetProperty("exclusions");

        Assert.True(exclusions.GetProperty("partLoadCurvesExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("seasonalEfficiencyExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("storageLossesExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("controlLossesExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("emissionLossesExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("multipleGeneratorsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("renewableFractionExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("heatPumpCopModelExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("en15316DetailedSubsystemMethodExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("distributionTemperatureLevelsExcluded").GetBoolean());
        Assert.True(exclusions.GetProperty("hourlyOperationProfileExcluded").GetBoolean());
    }

    [Fact]
    public void ExpectedOutputMatchesIndependentManualFormulaWithinTolerance()
    {
        using var input = ReadJson(InputPath);
        using var expected = ReadJson(ExpectedOutputPath);
        using var tolerances = ReadJson(TolerancesPath);

        var inputRoot = input.RootElement;
        var expectedRoot = expected.RootElement;

        var useful = inputRoot.GetProperty("usefulThermalDemandKWh").GetDouble();
        var distributionEfficiency = inputRoot.GetProperty("distributionEfficiency").GetDouble();
        var generationEfficiency = inputRoot.GetProperty("generationEfficiency").GetDouble();
        var auxiliaryElectricity = inputRoot.GetProperty("auxiliaryElectricityKWh").GetDouble();
        var fuelPeFactor = inputRoot.GetProperty("fuelPrimaryEnergyFactor").GetDouble();
        var electricityPeFactor = inputRoot.GetProperty("electricityPrimaryEnergyFactor").GetDouble();

        var generatorOutput = useful / distributionEfficiency;
        var fuelFinal = generatorOutput / generationEfficiency;
        var distributionLosses = generatorOutput - useful;
        var generationLosses = fuelFinal - generatorOutput;
        var totalThermalLosses = distributionLosses + generationLosses;
        var totalFinal = fuelFinal + auxiliaryElectricity;
        var fuelPrimary = fuelFinal * fuelPeFactor;
        var auxiliaryPrimary = auxiliaryElectricity * electricityPeFactor;
        var totalPrimary = fuelPrimary + auxiliaryPrimary;

        var absTol = tolerances.RootElement.GetProperty("absoluteToleranceKWh").GetDouble();
        var relTol = tolerances.RootElement.GetProperty("relativeTolerance").GetDouble();

        AssertClose(useful, expectedRoot.GetProperty("usefulThermalDemandKWh").GetDouble(), absTol, relTol);
        AssertClose(distributionEfficiency, expectedRoot.GetProperty("distributionEfficiency").GetDouble(), absTol, relTol);
        AssertClose(generationEfficiency, expectedRoot.GetProperty("generationEfficiency").GetDouble(), absTol, relTol);
        AssertClose(generatorOutput, expectedRoot.GetProperty("generatorOutputThermalEnergyKWh").GetDouble(), absTol, relTol);
        AssertClose(fuelFinal, expectedRoot.GetProperty("fuelFinalEnergyKWh").GetDouble(), absTol, relTol);
        AssertClose(distributionLosses, expectedRoot.GetProperty("distributionLossesKWh").GetDouble(), absTol, relTol);
        AssertClose(generationLosses, expectedRoot.GetProperty("generationLossesKWh").GetDouble(), absTol, relTol);
        AssertClose(totalThermalLosses, expectedRoot.GetProperty("totalThermalSystemLossesKWh").GetDouble(), absTol, relTol);
        AssertClose(auxiliaryElectricity, expectedRoot.GetProperty("auxiliaryElectricityKWh").GetDouble(), absTol, relTol);
        AssertClose(totalFinal, expectedRoot.GetProperty("totalFinalEnergyKWh").GetDouble(), absTol, relTol);
        AssertClose(fuelPeFactor, expectedRoot.GetProperty("fuelPrimaryEnergyFactor").GetDouble(), absTol, relTol);
        AssertClose(electricityPeFactor, expectedRoot.GetProperty("electricityPrimaryEnergyFactor").GetDouble(), absTol, relTol);
        AssertClose(fuelPrimary, expectedRoot.GetProperty("fuelPrimaryEnergyKWh").GetDouble(), absTol, relTol);
        AssertClose(auxiliaryPrimary, expectedRoot.GetProperty("auxiliaryElectricityPrimaryEnergyKWh").GetDouble(), absTol, relTol);
        AssertClose(totalPrimary, expectedRoot.GetProperty("totalPrimaryEnergyKWh").GetDouble(), absTol, relTol);
    }

    [Fact]
    public void DerivationContainsFormulaExpectedValuesAndNonClaims()
    {
        var content = File.ReadAllText(DerivationPath);

        var requiredPhrases = new[]
        {
            "Q_generator_output",
            "1000.0 / 0.95",
            "1169.5906432748538 kWh",
            "1194.5906432748538 kWh",
            "1349.0497076023392 kWh",
            "No EN 15316 full validation claim",
            "No detailed system-energy validation claim"
        };

        foreach (var phrase in requiredPhrases)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualFixtureIndexContainsSystemEnergyCase()
    {
        var content = File.ReadAllText(ManualIndexPath);

        Assert.Contains("MAN-ENG-SYS-001", content, StringComparison.Ordinal);
        Assert.Contains("1169.5906432748538 kWh", content, StringComparison.Ordinal);
        Assert.Contains("1194.5906432748538 kWh", content, StringComparison.Ordinal);
        Assert.Contains("1349.0497076023392 kWh", content, StringComparison.Ordinal);
        Assert.Contains("MAN-ENG-SYS-001-useful-to-final-energy-chain", content, StringComparison.Ordinal);
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
            "MAN-ENG-SYS-001-useful-to-final-energy-chain");

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
