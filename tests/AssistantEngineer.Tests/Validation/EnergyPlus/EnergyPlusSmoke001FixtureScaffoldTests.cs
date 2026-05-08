using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusSmoke001FixtureScaffoldTests
{
    [Fact]
    public void FixtureFilesDocumentationAndReadinessReportExist()
    {
        var requiredFiles = new[]
        {
            CaseMetadataPath,
            AssistantEngineerInputPath,
            ReferenceOutputPlaceholderPath,
            ComparisonTolerancesPath,
            FixtureReadmePath,
            ReadinessGeneratorPath,
            ReadinessReportPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required EP-SMOKE-001 fixture artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void CaseMetadataDeclaresSmokePlaceholderAndRequiredNonClaims()
    {
        using var document = ReadJson(CaseMetadataPath);
        var root = document.RootElement;

        Assert.Equal("EP-SMOKE-001", root.GetProperty("caseId").GetString());
        Assert.Equal("Smoke", root.GetProperty("stage").GetString());
        Assert.Equal("ReferenceFixturePlaceholder", root.GetProperty("status").GetString());

        Assert.Contains(
            "not exact EnergyPlus comparison workflow",
            root.GetProperty("purpose").GetString(),
            StringComparison.OrdinalIgnoreCase);

        AssertContainsArrayItem(
            root.GetProperty("notAClaim"),
            "Does not claim exact EnergyPlus numerical equivalence.");

        AssertContainsArrayItem(
            root.GetProperty("notAClaim"),
            "Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");
    }

    [Fact]
    public void AssistantEngineerInputContainsExpectedTransmissionOnlyHeatingFormulaValues()
    {
        using var document = ReadJson(AssistantEngineerInputPath);
        var root = document.RootElement;

        Assert.Equal("EP-SMOKE-001", root.GetProperty("caseId").GetString());
        Assert.Equal("AssistantEngineer", root.GetProperty("engine").GetString());
        Assert.Equal("EngineeringCoreV1SmokeFixture", root.GetProperty("inputKind").GetString());

        Assert.Equal(50.0, root.GetProperty("building").GetProperty("floorAreaM2").GetDouble());
        Assert.Equal(180.0, root.GetProperty("envelope").GetProperty("opaqueAreaM2").GetDouble());
        Assert.Equal(0.35, root.GetProperty("envelope").GetProperty("uValueWPerM2K").GetDouble());
        Assert.Equal(20.0, root.GetProperty("zone").GetProperty("indoorHeatingSetpointC").GetDouble());
        Assert.Equal(-5.0, root.GetProperty("weather").GetProperty("outdoorDryBulbC").GetDouble());

        Assert.Equal(
            "U * A * (T_indoor - T_outdoor)",
            root.GetProperty("calculationFormula").GetProperty("transmissionHeatLossW").GetString());

        Assert.Equal(
            1575.0,
            root.GetProperty("calculationFormula").GetProperty("expectedTransmissionHeatLossW").GetDouble());

        Assert.Equal(
            37.8,
            root.GetProperty("calculationFormula").GetProperty("expectedDailyHeatingEnergyKwh").GetDouble());
    }

    [Fact]
    public void ReferenceOutputIsExplicitPlaceholderAndNotValidationClaim()
    {
        using var document = ReadJson(ReferenceOutputPlaceholderPath);
        var root = document.RootElement;

        Assert.Equal("EP-SMOKE-001", root.GetProperty("caseId").GetString());
        Assert.Equal("EnergyPlus", root.GetProperty("referenceEngine").GetString());
        Assert.Equal("PlaceholderReferenceOutput", root.GetProperty("status").GetString());

        AssertContainsArrayItem(
            root.GetProperty("notAClaim"),
            "This placeholder does not claim EnergyPlus validation.");

        AssertContainsArrayItem(
            root.GetProperty("notAClaim"),
            "This placeholder does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");

        Assert.Equal(
            37.8,
            root.GetProperty("referenceOutputs").GetProperty("annualHeatingEnergyKwh").GetDouble());

        Assert.Equal(
            1575.0,
            root.GetProperty("referenceOutputs").GetProperty("peakHeatingLoadW").GetDouble());
    }

    [Fact]
    public void ComparisonTolerancesDefineMetricsAndNonClaims()
    {
        using var document = ReadJson(ComparisonTolerancesPath);
        var root = document.RootElement;

        Assert.Equal("EP-SMOKE-001", root.GetProperty("caseId").GetString());

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
            Assert.False(string.IsNullOrWhiteSpace(metric.GetProperty("type").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(metric.GetProperty("unit").GetString()));
            Assert.True(metric.GetProperty("tolerancePercent").GetDouble() >= 0);
            Assert.True(metric.GetProperty("absoluteTolerance").GetDouble() >= 0);
        }

        AssertContainsArrayItem(
            root.GetProperty("requiredNonClaims"),
            "Does not claim exact EnergyPlus numerical equivalence.");
    }

    [Fact]
    public void FixtureReadmeDocumentsFutureRealEnergyPlusWorkAndNonClaims()
    {
        var content = File.ReadAllText(FixtureReadmePath);

        var requiredPhrases = new[]
        {
            "ReferenceFixturePlaceholder",
            "not a real EnergyPlus validation result yet",
            "Q = U * A * ?T",
            "Expected transmission heat loss = 1575 W",
            "Expected daily heating energy = 37.8 kWh",
            "Future real EnergyPlus fixture",
            "exact EnergyPlus numerical equivalence",
            "ASHRAE 140 / BESTEST-style validation anchor coverage"
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
    public void ReadinessReportDocumentsPlaceholderStatusMetricsAndNonClaims()
    {
        var content = File.ReadAllText(ReadinessReportPath);

        Assert.Contains("EP-SMOKE-001 Comparison Readiness", content, StringComparison.Ordinal);
        Assert.Contains("ReferenceFixturePlaceholder", content, StringComparison.Ordinal);
        Assert.Contains("PlaceholderReferenceOutput", content, StringComparison.Ordinal);
        Assert.Contains("annual-heating-kwh", content, StringComparison.Ordinal);
        Assert.Contains("peak-heating-w", content, StringComparison.Ordinal);
        Assert.Contains("not a real EnergyPlus comparison yet", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must not claim exact EnergyPlus comparison workflow", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GeneratorScriptReadsFixtureFilesAndWritesReadinessReport()
    {
        var content = File.ReadAllText(ReadinessGeneratorPath);

        Assert.Contains("case-metadata.json", content, StringComparison.Ordinal);
        Assert.Contains("assistantengineer-input.json", content, StringComparison.Ordinal);
        Assert.Contains("reference-output.placeholder.json", content, StringComparison.Ordinal);
        Assert.Contains("comparison-tolerances.json", content, StringComparison.Ordinal);
        Assert.Contains("EP-SMOKE-001-ComparisonReadiness.md", content, StringComparison.Ordinal);
    }

    private static void AssertContainsArrayItem(
        JsonElement array,
        string expected)
    {
        var values = array
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(expected, values);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string FixtureDirectory =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "validation",
            "energyplus",
            "EP-SMOKE-001");

    private static string CaseMetadataPath =>
        Path.Combine(FixtureDirectory, "case-metadata.json");

    private static string AssistantEngineerInputPath =>
        Path.Combine(FixtureDirectory, "assistantengineer-input.json");

    private static string ReferenceOutputPlaceholderPath =>
        Path.Combine(FixtureDirectory, "reference-output.placeholder.json");

    private static string ComparisonTolerancesPath =>
        Path.Combine(FixtureDirectory, "comparison-tolerances.json");

    private static string FixtureReadmePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "fixtures",
            "EP-SMOKE-001",
            "README.md");

    private static string ReadinessGeneratorPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "generate-ep-smoke-001-comparison-readiness.ps1");

    private static string ReadinessReportPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "validation",
            "EP-SMOKE-001-ComparisonReadiness.md");
}
