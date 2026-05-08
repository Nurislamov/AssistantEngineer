using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationGenericComparisonRunnerTests
{
    [Fact]
    public void GenericRunnerScriptDocumentationAndSummaryExist()
    {
        var requiredFiles = new[]
        {
            GenericRunnerScriptPath,
            GenericRunnerDocumentationPath,
            GenericSummaryJsonPath,
            GenericSummaryMarkdownPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required generic validation runner artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void GenericRunnerScriptDiscoversFixturesSupportsPlaceholderAndRealReferencesAndWritesReports()
    {
        var content = File.ReadAllText(GenericRunnerScriptPath);

        var requiredPhrases = new[]
        {
            "tests/fixtures/validation/energyplus",
            "comparison-tolerances.json",
            "assistantengineer-input.json",
            "reference-output.placeholder.json",
            "energyplus-output.reference.json",
            "RequireRealReferences",
            "GenericEnergyPlusValidationFixtureRunner",
            "PlaceholderComparison",
            "RealEnergyPlusComparison",
            "NumericWithinTolerance",
            "SameSign",
            "DirectionalTrend",
            "EnergyPlusValidationGenericComparisonSummary.json",
            "EnergyPlusValidationGenericComparisonSummary.md"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GenericSummaryDeclaresRunnerStatusTotalsAndNonClaims()
    {
        using var document = ReadJson(GenericSummaryJsonPath);
        var root = document.RootElement;

        Assert.Equal(
            "Generic EnergyPlus Validation Fixture Comparison Summary",
            root.GetProperty("summaryName").GetString());

        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("PlannedValidation", root.GetProperty("status").GetString());
        Assert.Equal("GenericEnergyPlusValidationFixtureRunner", root.GetProperty("runner").GetString());

        var totals = root.GetProperty("totals");

        Assert.True(totals.GetProperty("fixturesDiscovered").GetInt32() >= 1);
        Assert.True(totals.GetProperty("comparisonsGenerated").GetInt32() >= 1);
        Assert.True(totals.GetProperty("allPassingComparisons").GetInt32() >= 1);
        Assert.True(totals.GetProperty("placeholderComparisons").GetInt32() >= 1);

        var nonClaims = root
            .GetProperty("requiredNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim exact EnergyPlus", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim ASHRAE 140", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("PlaceholderComparison is not real EnergyPlus validation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenericSummaryIncludesEpSmoke001PlaceholderComparison()
    {
        using var document = ReadJson(GenericSummaryJsonPath);

        var epSmoke001 = document
            .RootElement
            .GetProperty("cases")
            .EnumerateArray()
            .Single(item => item.GetProperty("caseId").GetString() == "EP-SMOKE-001");

        Assert.Equal("Smoke", epSmoke001.GetProperty("stage").GetString());
        Assert.Equal("PlaceholderComparison", epSmoke001.GetProperty("comparisonStatus").GetString());
        Assert.Equal("PlaceholderReferenceOutput", epSmoke001.GetProperty("referenceStatus").GetString());
        Assert.Equal(3, epSmoke001.GetProperty("metricsTotal").GetInt32());
        Assert.Equal(3, epSmoke001.GetProperty("metricsPassed").GetInt32());
        Assert.Equal(0, epSmoke001.GetProperty("metricsFailed").GetInt32());
        Assert.True(epSmoke001.GetProperty("allMetricsPassed").GetBoolean());
    }

    [Fact]
    public void EpSmoke001ComparisonResultWasGeneratedByGenericRunnerAndRemainsCompatible()
    {
        using var document = ReadJson(EpSmoke001ComparisonResultJsonPath);
        var root = document.RootElement;

        Assert.Equal("EP-SMOKE-001", root.GetProperty("caseId").GetString());
        Assert.Equal("GenericEnergyPlusValidationFixtureRunner", root.GetProperty("comparisonRunner").GetString());
        Assert.Equal("PlaceholderComparison", root.GetProperty("comparisonStatus").GetString());
        Assert.Equal("PlaceholderReferenceOutput", root.GetProperty("referenceStatus").GetString());
        Assert.True(root.GetProperty("allMetricsPassed").GetBoolean());

        Assert.Contains(
            "not a real EnergyPlus validation",
            root.GetProperty("interpretation").GetString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenericSummaryMarkdownDocumentsStatusCasesAndNonClaims()
    {
        var content = File.ReadAllText(GenericSummaryMarkdownPath);

        var requiredPhrases = new[]
        {
            "Generic EnergyPlus Validation Fixture Comparison Summary",
            "GenericEnergyPlusValidationFixtureRunner",
            "PlannedValidation",
            "EP-SMOKE-001",
            "PlaceholderComparison",
            "PlaceholderReferenceOutput",
            "not real EnergyPlus validation",
            "does not claim exact EnergyPlus numerical equivalence",
            "does not claim ASHRAE 140 / BESTEST-style validation anchor coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GenericRunnerDocumentationExplainsConventionStrictModeGeneratedOutputsAndNonClaims()
    {
        var content = File.ReadAllText(GenericRunnerDocumentationPath);

        var requiredPhrases = new[]
        {
            "Generic EnergyPlus Validation Fixture Runner",
            "compare-energyplus-validation-fixtures.ps1",
            "-RequireRealReferences",
            "reference-output.placeholder.json",
            "energyplus-output.reference.json",
            "NumericWithinTolerance",
            "SameSign",
            "DirectionalTrend",
            "EP-SMOKE-001",
            "PlaceholderComparison",
            "not real EnergyPlus validation",
            "Future real validation must remain tolerance-based"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RegenerateArtifactsScriptRunsGenericValidationRunner()
    {
        var content = File.ReadAllText(RegenerateArtifactsScriptPath);

        Assert.Contains(
            "compare-energyplus-validation-fixtures.ps1",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MainVerificationScriptIncludesGenericRunnerGuardTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains(
            "EnergyPlusValidationGenericComparisonRunnerTests",
            content,
            StringComparison.Ordinal);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string GenericRunnerScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "compare-energyplus-validation-fixtures.ps1");

    private static string GenericRunnerDocumentationPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "EnergyPlusValidationGenericRunner.md");

    private static string GenericSummaryJsonPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EnergyPlusValidationGenericComparisonSummary.json");

    private static string GenericSummaryMarkdownPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EnergyPlusValidationGenericComparisonSummary.md");

    private static string EpSmoke001ComparisonResultJsonPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EP-SMOKE-001-ComparisonResult.json");

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
