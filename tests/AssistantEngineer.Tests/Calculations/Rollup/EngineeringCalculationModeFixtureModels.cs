using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

namespace AssistantEngineer.Tests.Calculations.Rollup;

internal sealed record EngineeringCalculationModeComparisonFixture(
    string Id,
    EngineeringCalculationModeDomain Domain,
    string CompatibilityModeId,
    string InspiredModeId,
    IReadOnlyList<string> ClaimBoundary,
    IReadOnlyList<EngineeringCalculationModeMetric> CompatibilityMetrics,
    IReadOnlyList<EngineeringCalculationModeMetric> InspiredMetrics,
    IReadOnlyDictionary<string, double>? AbsoluteTolerances,
    IReadOnlyDictionary<string, double>? RelativeTolerancesPercent,
    string ExpectedSummaryStatus,
    IReadOnlyList<EngineeringCalculationModeExpectedDelta> ExpectedDeltas);

internal sealed record EngineeringCalculationModeExpectedDelta(
    string MetricName,
    double AbsoluteDelta);

internal sealed record EngineeringCalculationModeCatalogFixture(
    string Id,
    IReadOnlyList<string> ExpectedStageIds,
    IReadOnlyList<string> ExpectedOptionFlags,
    IReadOnlyList<string> ClaimBoundary);

internal static class EngineeringCalculationModeFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "rollup", "engineering-calculation-modes");

    public static IReadOnlyList<EngineeringCalculationModeComparisonFixture> LoadComparisonFixtures()
    {
        if (!Directory.Exists(FixtureDirectory))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {FixtureDirectory}");

        return Directory.GetFiles(FixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith("rollup-stage-catalog.json", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(path => JsonSerializer.Deserialize<EngineeringCalculationModeComparisonFixture>(
                File.ReadAllText(path),
                SerializerOptions) ?? throw new InvalidOperationException($"Fixture did not parse: {path}"))
            .ToArray();
    }

    public static EngineeringCalculationModeCatalogFixture LoadCatalogFixture()
    {
        var path = Path.Combine(FixtureDirectory, "rollup-stage-catalog.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Catalog fixture file was not found.", path);

        return JsonSerializer.Deserialize<EngineeringCalculationModeCatalogFixture>(
            File.ReadAllText(path),
            SerializerOptions) ?? throw new InvalidOperationException($"Fixture did not parse: {path}");
    }
}
