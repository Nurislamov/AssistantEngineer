using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationRealFixtureImportPipelineTests
{
    [Fact]
    public void ImportScriptAndGuideExist()
    {
        Assert.True(
            File.Exists(ImportScriptPath),
            $"Required EP-SMOKE-001 real fixture import script is missing: {ImportScriptPath}");

        Assert.True(
            File.Exists(ImportGuidePath),
            $"Required EP-SMOKE-001 real fixture import guide is missing: {ImportGuidePath}");
    }

    [Fact]
    public void ImportScriptRequiresRealSourceDirectoryAndEnergyPlusVersion()
    {
        var content = File.ReadAllText(ImportScriptPath);

        var requiredPhrases = new[]
        {
            "[Parameter(Mandatory = $true)]",
            "[string] $SourceDirectory",
            "[string] $EnergyPlusVersion",
            "EnergyPlus artifact source directory",
            "EnergyPlus IDF model",
            "EnergyPlus EPW weather",
            "EnergyPlus raw CSV output"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ImportScriptCreatesAllRequiredRealFixtureFiles()
    {
        var content = File.ReadAllText(ImportScriptPath);

        var requiredPhrases = new[]
        {
            "energyplus-model.idf",
            "weather.epw",
            "energyplus-output.raw.csv",
            "energyplus-output.reference.json",
            "provenance.json",
            "Copy-RequiredFile",
            "Get-FileHash",
            "SHA256"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ImportScriptNormalizesReferenceOutputsForComparisonRunner()
    {
        var content = File.ReadAllText(ImportScriptPath);

        var requiredPhrases = new[]
        {
            "referenceStatus = \"RealEnergyPlusReferenceOutput\"",
            "referenceOutputs",
            "annualHeatingEnergyKwh",
            "peakHeatingLoadW",
            "annualCoolingEnergyKwh",
            "detectedColumns",
            "ConvertTo-Json -Depth 20"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ImportScriptSupportsExplicitMetricsAndCsvColumnExtraction()
    {
        var content = File.ReadAllText(ImportScriptPath);

        var requiredPhrases = new[]
        {
            "$AnnualHeatingEnergyKwh",
            "$PeakHeatingLoadW",
            "$AnnualCoolingEnergyKwh",
            "$HeatingEnergyColumn",
            "$HeatingLoadColumn",
            "$CoolingEnergyColumn",
            "Import-Csv",
            "Convert-EnergySeriesToKwh",
            "Convert-LoadSeriesToW",
            "DistrictHeating",
            "DistrictCooling"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ImportScriptRunsStrictRealFixtureGateAndValidationProfileByDefault()
    {
        var content = File.ReadAllText(ImportScriptPath);

        var requiredPhrases = new[]
        {
            "[switch] $SkipValidation",
            "regenerate-engineering-core-v1-validation-artifacts.ps1",
            "assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture",
            "compare-energyplus-validation-fixtures.ps1 -RequireRealReferences",
            "verify-engineering-core-v1-validation.ps1"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ImportScriptKeepsDeterministicGeneratedTimestampAndNonClaims()
    {
        var content = File.ReadAllText(ImportScriptPath);

        Assert.Contains("2026-01-01 00:00:00 UTC", content, StringComparison.Ordinal);
        Assert.DoesNotContain("(Get-Date).ToUniversalTime()", content, StringComparison.Ordinal);

        var requiredPhrases = new[]
        {
            "Does not claim exact EnergyPlus numerical parity.",
            "Does not claim ASHRAE 140 validation coverage.",
            "Does not claim full ISO 52016 node/matrix solver parity.",
            "RealEnergyPlusComparison remains tolerance-based.",
            "does not claim exact numerical parity or ASHRAE 140 validation coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ImportGuideDocumentsSafeUsageAndWarnsAgainstFakeReferenceValues()
    {
        var content = File.ReadAllText(ImportGuidePath);

        var requiredPhrases = new[]
        {
            "EP-SMOKE-001 Real EnergyPlus Fixture Import Guide",
            "energyplus-model.idf",
            "weather.epw",
            "energyplus-output.raw.csv",
            "energyplus-output.reference.json",
            "provenance.json",
            "Import with explicit metric values",
            "Import by reading CSV columns",
            "Do not use AssistantEngineer expected values as fake EnergyPlus reference values.",
            "assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture",
            "compare-energyplus-validation-fixtures.ps1 -RequireRealReferences",
            "EP-SMOKE-001 = RealEnergyPlusComparison"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ImportScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "import-ep-smoke-001-real-fixture.ps1");

    private static string ImportGuidePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "fixtures",
            "EP-SMOKE-001",
            "RealEnergyPlusFixtureImportGuide.md");
}
