using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationComparisonSummaryTests
{
    [Fact]
    public void SummaryScriptReportsAndReadmeExist()
    {
        var requiredFiles = new[]
        {
            SummaryScriptPath,
            SummaryJsonPath,
            SummaryMarkdownPath,
            ValidationReportsReadmePath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required validation comparison summary artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void SummaryJsonDeclaresPlannedValidationAndUsesRegistryAndComparisonResult()
    {
        using var document = ReadJson(SummaryJsonPath);
        var root = document.RootElement;

        Assert.Equal(
            "Engineering Core V1 Validation Comparison Summary",
            root.GetProperty("summaryName").GetString());

        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("PlannedValidation", root.GetProperty("status").GetString());

        Assert.Equal(
            "docs/validation/EnergyPlusValidationCaseRegistry.json",
            root.GetProperty("registryFile").GetString());

        var resultFiles = root
            .GetProperty("comparisonResultFiles")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(
            "docs/reports/validation/EP-SMOKE-001-ComparisonResult.json",
            resultFiles);
    }

    [Fact]
    public void SummaryTotalsReflectRegistryAndPlaceholderComparison()
    {
        using var summary = ReadJson(SummaryJsonPath);
        using var registry = ReadJson(ValidationRegistryPath);

        var totals = summary.RootElement.GetProperty("totals");

        Assert.Equal(
            registry.RootElement.GetProperty("cases").GetArrayLength(),
            totals.GetProperty("totalCases").GetInt32());

        Assert.True(totals.GetProperty("casesWithComparison").GetInt32() >= 1);
        Assert.True(totals.GetProperty("casesPassing").GetInt32() >= 1);
        Assert.True(totals.GetProperty("placeholderComparisons").GetInt32() >= 1);
        Assert.True(totals.GetProperty("plannedOnly").GetInt32() >= 1);
    }

    [Fact]
    public void SummaryIncludesEpSmoke001AsPassingPlaceholderComparison()
    {
        using var document = ReadJson(SummaryJsonPath);

        var epSmoke001 = document
            .RootElement
            .GetProperty("cases")
            .EnumerateArray()
            .Single(item => item.GetProperty("caseId").GetString() == "EP-SMOKE-001");

        Assert.Equal("Smoke", epSmoke001.GetProperty("stage").GetString());
        Assert.Equal("PlaceholderComparison", epSmoke001.GetProperty("comparisonStatus").GetString());
        Assert.Equal("PlaceholderReferenceOutput", epSmoke001.GetProperty("referenceStatus").GetString());
        Assert.True(epSmoke001.GetProperty("hasComparisonResult").GetBoolean());
        Assert.True(epSmoke001.GetProperty("allMetricsPassed").GetBoolean());
        Assert.Equal(3, epSmoke001.GetProperty("metricsTotal").GetInt32());
        Assert.Equal(3, epSmoke001.GetProperty("metricsPassed").GetInt32());
        Assert.Equal(0, epSmoke001.GetProperty("metricsFailed").GetInt32());
    }

    [Fact]
    public void SummaryKeepsRequiredNonClaimsVisible()
    {
        using var document = ReadJson(SummaryJsonPath);

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
    public void SummaryMarkdownDocumentsCasesMetricsStatusAndNonClaims()
    {
        var content = File.ReadAllText(SummaryMarkdownPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 Validation Comparison Summary",
            "PlannedValidation",
            "EP-SMOKE-001",
            "PlaceholderComparison",
            "PlaceholderReferenceOutput",
            "Cases with comparison",
            "Placeholder comparisons",
            "Planned-only cases",
            "does not claim exact EnergyPlus numerical equivalence",
            "does not claim ASHRAE 140 / BESTEST-style validation anchor coverage",
            "Future real validation must use committed EnergyPlus/reference model files"
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
    public void ValidationReportsReadmeDocumentsGenerationAndPlaceholderStatus()
    {
        var content = File.ReadAllText(ValidationReportsReadmePath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 Validation Reports",
            "EP-SMOKE-001-ComparisonReadiness.md",
            "EP-SMOKE-001-ComparisonResult.json",
            "EngineeringCoreV1ValidationComparisonSummary.json",
            "generate-engineering-core-v1-validation-comparison-summary.ps1",
            "EP-SMOKE-001 = PlaceholderComparison",
            "PlaceholderComparison is not real EnergyPlus validation",
            "future real validation must remain tolerance-based",
            "EnergyPlusValidationComparisonSummaryTests"
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
    public void SummaryScriptReadsRegistryAndSmokeComparisonResultsAndWritesOutputs()
    {
        var content = File.ReadAllText(SummaryScriptPath);

        Assert.Contains("EnergyPlusValidationCaseRegistry.json", content, StringComparison.Ordinal);
        Assert.Contains("EP-SMOKE-*-ComparisonResult.json", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1ValidationComparisonSummary.json", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1ValidationComparisonSummary.md", content, StringComparison.Ordinal);
        Assert.Contains("PlaceholderComparison", content, StringComparison.Ordinal);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string SummaryScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "generate-engineering-core-v1-validation-comparison-summary.ps1");

    private static string SummaryJsonPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EngineeringCoreV1ValidationComparisonSummary.json");

    private static string SummaryMarkdownPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EngineeringCoreV1ValidationComparisonSummary.md");

    private static string ValidationReportsReadmePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "README.md");

    private static string ValidationRegistryPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "EnergyPlusValidationCaseRegistry.json");
}

