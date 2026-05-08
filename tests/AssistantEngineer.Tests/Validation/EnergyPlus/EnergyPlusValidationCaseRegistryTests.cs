using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationCaseRegistryTests
{
    [Fact]
    public void ValidationCaseRegistryAndReadinessReportExist()
    {
        var requiredFiles = new[]
        {
            RegistryPath,
            RegistryDocumentationPath,
            ReadinessReportPath,
            GeneratorScriptPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required validation registry file is missing: {requiredFile}");
        }
    }

    [Fact]
    public void RegistryDeclaresPlannedValidationAndNonParityPurpose()
    {
        using var registry = ReadRegistry();
        var root = registry.RootElement;

        Assert.Equal(
            "Engineering Core V1 EnergyPlus / ASHRAE 140-style Validation Case Registry",
            root.GetProperty("registryName").GetString());

        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("PlannedValidation", root.GetProperty("status").GetString());

        Assert.Contains(
            "not exact EnergyPlus comparison workflow",
            root.GetProperty("purpose").GetString(),
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "not ASHRAE 140 certification",
            root.GetProperty("purpose").GetString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistryCaseIdsAreUniqueAndContainExpectedInitialCases()
    {
        var cases = ReadCases();

        var duplicateCaseIds = cases
            .GroupBy(item => item.CaseId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateCaseIds.Length == 0,
            $"Validation case ids must be unique: {string.Join(", ", duplicateCaseIds)}.");

        var ids = cases
            .Select(item => item.CaseId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("EP-SMOKE-001", ids);
        Assert.Contains("EP-SMOKE-002", ids);
        Assert.Contains("EP-SMOKE-003", ids);
        Assert.Contains("ASHRAE140-STYLE-001", ids);
        Assert.Contains("ASHRAE140-STYLE-002", ids);
    }

    [Fact]
    public void RegistryContainsRequiredMetadataForEveryCase()
    {
        var violations = ReadCases()
            .Where(item =>
                string.IsNullOrWhiteSpace(item.CaseId) ||
                string.IsNullOrWhiteSpace(item.Name) ||
                string.IsNullOrWhiteSpace(item.Stage) ||
                string.IsNullOrWhiteSpace(item.Status) ||
                string.IsNullOrWhiteSpace(item.Source) ||
                string.IsNullOrWhiteSpace(item.WeatherSource) ||
                string.IsNullOrWhiteSpace(item.Geometry) ||
                string.IsNullOrWhiteSpace(item.Envelope) ||
                string.IsNullOrWhiteSpace(item.InternalGains) ||
                string.IsNullOrWhiteSpace(item.Ventilation) ||
                string.IsNullOrWhiteSpace(item.HvacControl) ||
                item.Metrics.Count == 0 ||
                item.Assumptions.Count == 0 ||
                item.KnownDifferences.Count == 0 ||
                item.NonClaims.Count == 0)
            .Select(item => item.CaseId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Validation cases must include required metadata: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void RegistryMetricsUseAllowedTypesAndHaveUnitsToleranceAndDirection()
    {
        var allowedTypes = new HashSet<string>(
            ["NumericWithinTolerance", "DirectionalTrend", "SameSign"],
            StringComparer.Ordinal);

        var violations = ReadCases()
            .SelectMany(item => item.Metrics.Select(metric => new
            {
                item.CaseId,
                Metric = metric
            }))
            .Where(item =>
                string.IsNullOrWhiteSpace(item.Metric.MetricId) ||
                string.IsNullOrWhiteSpace(item.Metric.Name) ||
                string.IsNullOrWhiteSpace(item.Metric.Unit) ||
                string.IsNullOrWhiteSpace(item.Metric.Type) ||
                string.IsNullOrWhiteSpace(item.Metric.Direction) ||
                !allowedTypes.Contains(item.Metric.Type) ||
                item.Metric.TolerancePercent < 0)
            .Select(item => $"{item.CaseId}:{item.Metric.MetricId}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Validation metrics must include id/name/unit/type/tolerance/direction and use allowed types: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void EveryCaseKeepsRequiredNonClaimsVisible()
    {
        var cases = ReadCases();

        foreach (var validationCase in cases)
        {
            Assert.Contains(
                validationCase.NonClaims,
                claim => claim.Contains(
                    "Does not claim exact EnergyPlus",
                    StringComparison.OrdinalIgnoreCase));

            Assert.Contains(
                validationCase.NonClaims,
                claim => claim.Contains(
                    "Does not claim ASHRAE 140",
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void RegistryContainsSmokeAndAshrae140StyleStages()
    {
        var stages = ReadCases()
            .Select(item => item.Stage)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Smoke", stages);
        Assert.Contains("Ashrae140Style", stages);
    }

    [Fact]
    public void ReadinessReportContainsSummaryCasesMetricsTolerancesAndNonClaims()
    {
        var content = File.ReadAllText(ReadinessReportPath);

        Assert.Contains("Engineering Core V1 Validation Readiness", content, StringComparison.Ordinal);
        Assert.Contains("Registry summary", content, StringComparison.Ordinal);
        Assert.Contains("Default tolerances", content, StringComparison.Ordinal);
        Assert.Contains("EP-SMOKE-001", content, StringComparison.Ordinal);
        Assert.Contains("ASHRAE140-STYLE-001", content, StringComparison.Ordinal);
        Assert.Contains("Required non-claims", content, StringComparison.Ordinal);
        Assert.Contains("not exact EnergyPlus numerical equivalence", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not ASHRAE 140 certification", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistryDocumentationExplainsStatusStagesRequiredMetadataGenerationAndGuardTests()
    {
        var content = File.ReadAllText(RegistryDocumentationPath);

        var requiredPhrases = new[]
        {
            "PlannedValidation",
            "not exact EnergyPlus comparison workflow",
            "not ASHRAE 140 certification",
            "Required case metadata",
            "Required metric metadata",
            "NumericWithinTolerance",
            "DirectionalTrend",
            "SameSign",
            "generate-engineering-core-v1-validation-readiness.ps1",
            "EnergyPlusValidationCaseRegistryTests"
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
    public void GeneratorScriptReadsRegistryAndWritesReadinessReport()
    {
        var content = File.ReadAllText(GeneratorScriptPath);

        Assert.Contains("EnergyPlusValidationCaseRegistry.json", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1ValidationReadiness.md", content, StringComparison.Ordinal);
        Assert.Contains("Default tolerances", content, StringComparison.Ordinal);
        Assert.Contains("Required non-claims", content, StringComparison.Ordinal);
    }

    private static JsonDocument ReadRegistry() =>
        JsonDocument.Parse(File.ReadAllText(RegistryPath));

    private static IReadOnlyList<ValidationRegistryCase> ReadCases()
    {
        using var registry = ReadRegistry();

        return registry
            .RootElement
            .GetProperty("cases")
            .EnumerateArray()
            .Select(item => new ValidationRegistryCase(
                CaseId: item.GetProperty("caseId").GetString() ?? string.Empty,
                Name: item.GetProperty("name").GetString() ?? string.Empty,
                Stage: item.GetProperty("stage").GetString() ?? string.Empty,
                Status: item.GetProperty("status").GetString() ?? string.Empty,
                Source: item.GetProperty("source").GetString() ?? string.Empty,
                WeatherSource: item.GetProperty("weatherSource").GetString() ?? string.Empty,
                Geometry: item.GetProperty("geometry").GetString() ?? string.Empty,
                Envelope: item.GetProperty("envelope").GetString() ?? string.Empty,
                InternalGains: item.GetProperty("internalGains").GetString() ?? string.Empty,
                Ventilation: item.GetProperty("ventilation").GetString() ?? string.Empty,
                HvacControl: item.GetProperty("hvacControl").GetString() ?? string.Empty,
                Metrics: item
                    .GetProperty("metrics")
                    .EnumerateArray()
                    .Select(metric => new ValidationRegistryMetric(
                        MetricId: metric.GetProperty("metricId").GetString() ?? string.Empty,
                        Name: metric.GetProperty("name").GetString() ?? string.Empty,
                        Unit: metric.GetProperty("unit").GetString() ?? string.Empty,
                        Type: metric.GetProperty("type").GetString() ?? string.Empty,
                        TolerancePercent: metric.GetProperty("tolerancePercent").GetDouble(),
                        Direction: metric.GetProperty("direction").GetString() ?? string.Empty))
                    .ToArray(),
                Assumptions: ReadStringArray(item, "assumptions"),
                KnownDifferences: ReadStringArray(item, "knownDifferences"),
                NonClaims: ReadStringArray(item, "nonClaims")))
            .ToArray();
    }

    private static string[] ReadStringArray(
        JsonElement root,
        string propertyName) =>
        root
            .GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

    private sealed record ValidationRegistryCase(
        string CaseId,
        string Name,
        string Stage,
        string Status,
        string Source,
        string WeatherSource,
        string Geometry,
        string Envelope,
        string InternalGains,
        string Ventilation,
        string HvacControl,
        IReadOnlyList<ValidationRegistryMetric> Metrics,
        IReadOnlyList<string> Assumptions,
        IReadOnlyList<string> KnownDifferences,
        IReadOnlyList<string> NonClaims);

    private sealed record ValidationRegistryMetric(
        string MetricId,
        string Name,
        string Unit,
        string Type,
        double TolerancePercent,
        string Direction);

    private static string RegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusValidationCaseRegistry.json");

    private static string RegistryDocumentationPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusValidationCaseRegistry.md");

    private static string ReadinessReportPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "EngineeringCoreV1ValidationReadiness.md");

    private static string GeneratorScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-validation-readiness.ps1");
}
