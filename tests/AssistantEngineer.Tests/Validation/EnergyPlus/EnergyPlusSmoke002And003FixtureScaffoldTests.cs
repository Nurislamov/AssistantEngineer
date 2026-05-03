using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusSmoke002And003FixtureScaffoldTests
{
    [Theory]
    [InlineData("EP-SMOKE-002")]
    [InlineData("EP-SMOKE-003")]
    public void SmokeFixtureFilesExist(string caseId)
    {
        var fixtureDirectory = FixtureDirectory(caseId);

        var requiredFiles = new[]
        {
            Path.Combine(fixtureDirectory, "case-metadata.json"),
            Path.Combine(fixtureDirectory, "assistantengineer-input.json"),
            Path.Combine(fixtureDirectory, "reference-output.placeholder.json"),
            Path.Combine(fixtureDirectory, "comparison-tolerances.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "fixtures", caseId, "README.md")
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required {caseId} fixture file is missing: {requiredFile}");
        }
    }

    [Fact]
    public void EpSmoke002DocumentsSolarCoolingScopeAndNonClaims()
    {
        using var metadata = ReadJson(Path.Combine(FixtureDirectory("EP-SMOKE-002"), "case-metadata.json"));
        using var input = ReadJson(Path.Combine(FixtureDirectory("EP-SMOKE-002"), "assistantengineer-input.json"));
        using var tolerances = ReadJson(Path.Combine(FixtureDirectory("EP-SMOKE-002"), "comparison-tolerances.json"));

        Assert.Equal("EP-SMOKE-002", metadata.RootElement.GetProperty("caseId").GetString());
        Assert.Equal("Smoke", metadata.RootElement.GetProperty("stage").GetString());
        Assert.Equal("ReferenceFixturePlaceholder", metadata.RootElement.GetProperty("status").GetString());

        Assert.Contains(
            "not exact EnergyPlus parity",
            metadata.RootElement.GetProperty("purpose").GetString(),
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            2860.0,
            input.RootElement.GetProperty("calculationFormula").GetProperty("expectedPeakWindowSolarGainW").GetDouble());

        Assert.Equal(
            3600.0,
            input.RootElement.GetProperty("calculationFormula").GetProperty("expectedPeakCoolingLoadW").GetDouble());

        Assert.Equal(
            18.0,
            input.RootElement.GetProperty("calculationFormula").GetProperty("expectedDailyCoolingEnergyKwh").GetDouble());

        var metricIds = MetricIds(tolerances.RootElement);

        Assert.Contains("annual-cooling-kwh", metricIds);
        Assert.Contains("peak-cooling-w", metricIds);
        Assert.Contains("solar-orientation-response", metricIds);
        Assert.Contains("annual-heating-kwh", metricIds);
    }

    [Fact]
    public void EpSmoke003DocumentsInternalGainsCoolingScopeAndNonClaims()
    {
        using var metadata = ReadJson(Path.Combine(FixtureDirectory("EP-SMOKE-003"), "case-metadata.json"));
        using var input = ReadJson(Path.Combine(FixtureDirectory("EP-SMOKE-003"), "assistantengineer-input.json"));
        using var tolerances = ReadJson(Path.Combine(FixtureDirectory("EP-SMOKE-003"), "comparison-tolerances.json"));

        Assert.Equal("EP-SMOKE-003", metadata.RootElement.GetProperty("caseId").GetString());
        Assert.Equal("Smoke", metadata.RootElement.GetProperty("stage").GetString());
        Assert.Equal("ReferenceFixturePlaceholder", metadata.RootElement.GetProperty("status").GetString());

        Assert.Contains(
            "not exact EnergyPlus parity",
            metadata.RootElement.GetProperty("purpose").GetString(),
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            1200.0,
            input.RootElement.GetProperty("internalGains").GetProperty("sensibleW").GetDouble());

        Assert.Equal(
            1200.0,
            input.RootElement.GetProperty("calculationFormula").GetProperty("expectedPeakCoolingLoadW").GetDouble());

        Assert.Equal(
            28.8,
            input.RootElement.GetProperty("calculationFormula").GetProperty("expectedDailyCoolingEnergyKwh").GetDouble());

        var metricIds = MetricIds(tolerances.RootElement);

        Assert.Contains("annual-cooling-kwh", metricIds);
        Assert.Contains("peak-cooling-w", metricIds);
        Assert.Contains("internal-gain-response", metricIds);
        Assert.Contains("annual-heating-kwh", metricIds);
    }

    [Theory]
    [InlineData("EP-SMOKE-002")]
    [InlineData("EP-SMOKE-003")]
    public void SmokeFixturePlaceholderReferenceKeepsNonClaimsVisible(string caseId)
    {
        using var document = ReadJson(Path.Combine(FixtureDirectory(caseId), "reference-output.placeholder.json"));

        Assert.Equal(caseId, document.RootElement.GetProperty("caseId").GetString());
        Assert.Equal("EnergyPlus", document.RootElement.GetProperty("referenceEngine").GetString());
        Assert.Equal("PlaceholderReferenceOutput", document.RootElement.GetProperty("status").GetString());

        var nonClaims = document
            .RootElement
            .GetProperty("notAClaim")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, claim => claim.Contains("does not claim EnergyPlus validation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("does not claim exact EnergyPlus", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("does not claim ASHRAE 140", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("EP-SMOKE-002")]
    [InlineData("EP-SMOKE-003")]
    public void SmokeFixtureComparisonResultGeneratedByGenericRunnerAndPasses(string caseId)
    {
        using var result = ReadJson(Path.Combine(TestPaths.RepoRoot, "docs", "reports", "validation", $"{caseId}-ComparisonResult.json"));

        Assert.Equal(caseId, result.RootElement.GetProperty("caseId").GetString());
        Assert.Equal("GenericEnergyPlusValidationFixtureRunner", result.RootElement.GetProperty("comparisonRunner").GetString());
        Assert.Equal("PlaceholderComparison", result.RootElement.GetProperty("comparisonStatus").GetString());
        Assert.Equal("PlaceholderReferenceOutput", result.RootElement.GetProperty("referenceStatus").GetString());
        Assert.True(result.RootElement.GetProperty("allMetricsPassed").GetBoolean());

        var metrics = result
            .RootElement
            .GetProperty("metrics")
            .EnumerateArray()
            .ToArray();

        Assert.True(metrics.Length >= 4);
        Assert.All(metrics, metric => Assert.True(metric.GetProperty("passed").GetBoolean()));
    }

    [Fact]
    public void GenericSummaryIncludesAllThreeSmokeFixtures()
    {
        using var summary = ReadJson(GenericSummaryPath);

        var caseIds = summary
            .RootElement
            .GetProperty("cases")
            .EnumerateArray()
            .Select(item => item.GetProperty("caseId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("EP-SMOKE-001", caseIds);
        Assert.Contains("EP-SMOKE-002", caseIds);
        Assert.Contains("EP-SMOKE-003", caseIds);

        var totals = summary.RootElement.GetProperty("totals");

        Assert.True(totals.GetProperty("fixturesDiscovered").GetInt32() >= 3);
        Assert.True(totals.GetProperty("comparisonsGenerated").GetInt32() >= 3);
        Assert.True(totals.GetProperty("placeholderComparisons").GetInt32() >= 3);
    }

    [Theory]
    [InlineData("EP-SMOKE-002", "solar cooling smoke fixture")]
    [InlineData("EP-SMOKE-003", "internal sensible gains cooling smoke fixture")]
    public void FixtureReadmesDocumentScopeStatusAndNonClaims(string caseId, string expectedScope)
    {
        var content = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "fixtures", caseId, "README.md"));

        Assert.Contains(expectedScope, content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReferenceFixturePlaceholder", content, StringComparison.Ordinal);
        Assert.Contains("PlaceholderComparison", content, StringComparison.Ordinal);
        Assert.Contains("exact EnergyPlus numerical parity", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASHRAE 140 validation coverage", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string FixtureDirectory(string caseId) =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "validation", "energyplus", caseId);

    private static string GenericSummaryPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EnergyPlusValidationGenericComparisonSummary.json");

    private static string[] MetricIds(JsonElement toleranceRoot) =>
        toleranceRoot
            .GetProperty("metrics")
            .EnumerateArray()
            .Select(item => item.GetProperty("metricId").GetString() ?? string.Empty)
            .ToArray();

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));
}
