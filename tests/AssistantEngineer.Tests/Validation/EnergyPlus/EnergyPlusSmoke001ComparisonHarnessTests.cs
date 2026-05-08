using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusSmoke001ComparisonHarnessTests
{
    [Fact]
    public void ComparisonScriptAndResultFilesExist()
    {
        var requiredFiles = new[]
        {
            ComparisonScriptPath,
            ComparisonResultJsonPath,
            ComparisonResultMarkdownPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required EP-SMOKE-001 comparison harness artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void ComparisonResultDeclaresPlaceholderComparisonAndNonValidationInterpretation()
    {
        using var document = ReadJson(ComparisonResultJsonPath);
        var root = document.RootElement;

        Assert.Equal("EP-SMOKE-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Smoke", root.GetProperty("stage").GetString());
        Assert.Equal("PlaceholderComparison", root.GetProperty("comparisonStatus").GetString());
        Assert.Equal("PlaceholderReferenceOutput", root.GetProperty("referenceStatus").GetString());

        Assert.Contains(
            "not a real EnergyPlus validation",
            root.GetProperty("interpretation").GetString(),
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "not an ASHRAE 140 / BESTEST-style validation anchor claim",
            root.GetProperty("interpretation").GetString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComparisonResultContainsExpectedMetricsAndAllPass()
    {
        using var document = ReadJson(ComparisonResultJsonPath);
        var root = document.RootElement;

        Assert.True(root.GetProperty("allMetricsPassed").GetBoolean());

        var metrics = root
            .GetProperty("metrics")
            .EnumerateArray()
            .ToArray();

        Assert.NotEmpty(metrics);

        var metricIds = metrics
            .Select(item => item.GetProperty("metricId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("annual-heating-kwh", metricIds);
        Assert.Contains("peak-heating-w", metricIds);
        Assert.Contains("annual-cooling-kwh", metricIds);

        foreach (var metric in metrics)
        {
            Assert.True(metric.GetProperty("passed").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(metric.GetProperty("type").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(metric.GetProperty("unit").GetString()));
            Assert.True(metric.GetProperty("absoluteDifference").GetDouble() >= 0);
            Assert.True(metric.GetProperty("effectiveAbsoluteTolerance").GetDouble() >= 0);
        }
    }

    [Fact]
    public void HeatingMetricMatchesExpectedPlaceholderValues()
    {
        using var document = ReadJson(ComparisonResultJsonPath);

        var heating = document
            .RootElement
            .GetProperty("metrics")
            .EnumerateArray()
            .Single(item => item.GetProperty("metricId").GetString() == "annual-heating-kwh");

        Assert.Equal(37.8, heating.GetProperty("assistantEngineerValue").GetDouble());
        Assert.Equal(37.8, heating.GetProperty("referenceValue").GetDouble());
        Assert.Equal(0.0, heating.GetProperty("absoluteDifference").GetDouble());
        Assert.True(heating.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public void PeakHeatingMetricMatchesExpectedPlaceholderValues()
    {
        using var document = ReadJson(ComparisonResultJsonPath);

        var peakHeating = document
            .RootElement
            .GetProperty("metrics")
            .EnumerateArray()
            .Single(item => item.GetProperty("metricId").GetString() == "peak-heating-w");

        Assert.Equal(1575.0, peakHeating.GetProperty("assistantEngineerValue").GetDouble());
        Assert.Equal(1575.0, peakHeating.GetProperty("referenceValue").GetDouble());
        Assert.Equal(0.0, peakHeating.GetProperty("absoluteDifference").GetDouble());
        Assert.True(peakHeating.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public void ComparisonResultKeepsRequiredNonClaimsVisible()
    {
        using var document = ReadJson(ComparisonResultJsonPath);

        var nonClaims = document
            .RootElement
            .GetProperty("requiredNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim exact EnergyPlus", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim ASHRAE 140", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim full ISO 52016", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ComparisonMarkdownDocumentsPlaceholderStatusMetricsAndFutureWork()
    {
        var content = File.ReadAllText(ComparisonResultMarkdownPath);

        var requiredPhrases = new[]
        {
            "EP-SMOKE-001 Comparison Result",
            "PlaceholderComparison",
            "PlaceholderReferenceOutput",
            "annual-heating-kwh",
            "peak-heating-w",
            "annual-cooling-kwh",
            "not a real EnergyPlus validation",
            "not ASHRAE 140 / BESTEST-style validation anchor coverage",
            "does not claim exact EnergyPlus numerical equivalence",
            "Future work must replace or supplement the placeholder reference"
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
    public void ComparisonScriptReadsFixtureFilesAndWritesJsonAndMarkdownResults()
    {
        var content = File.ReadAllText(ComparisonScriptPath);

        Assert.Contains("case-metadata.json", content, StringComparison.Ordinal);
        Assert.Contains("assistantengineer-input.json", content, StringComparison.Ordinal);
        Assert.Contains("reference-output.placeholder.json", content, StringComparison.Ordinal);
        Assert.Contains("comparison-tolerances.json", content, StringComparison.Ordinal);
        Assert.Contains("EP-SMOKE-001-ComparisonResult.json", content, StringComparison.Ordinal);
        Assert.Contains("EP-SMOKE-001-ComparisonResult.md", content, StringComparison.Ordinal);
        Assert.Contains("PlaceholderComparison", content, StringComparison.Ordinal);
        Assert.Contains("NumericWithinTolerance", content, StringComparison.Ordinal);
        Assert.Contains("SameSign", content, StringComparison.Ordinal);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string ComparisonScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "compare-ep-smoke-001-placeholder.ps1");

    private static string ComparisonResultJsonPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EP-SMOKE-001-ComparisonResult.json");

    private static string ComparisonResultMarkdownPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EP-SMOKE-001-ComparisonResult.md");
}
