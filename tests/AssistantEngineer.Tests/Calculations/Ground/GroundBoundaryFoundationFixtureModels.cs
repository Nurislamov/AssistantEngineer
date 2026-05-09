using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

internal sealed record GroundBoundaryFoundationFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    string Scenario,
    GroundBoundaryDefinition Boundary,
    GroundTemperatureProfileRequest GroundTemperatureProfileRequest,
    IReadOnlyList<double> ZoneIndoorTemperatureProfileCelsius,
    GroundBoundaryFoundationExpected Expected,
    IReadOnlyList<double>? ExteriorTemperatureProfileCelsius = null,
    IReadOnlyList<string>? ExpectedDiagnosticCodes = null);

internal sealed record GroundBoundaryFoundationExpected(
    double? EquivalentGroundHeatTransferCoefficientWPerKelvin = null,
    IReadOnlyList<double>? HeatFlowProfileWatts = null,
    double? AnnualHeatLossKiloWattHours = null,
    double? AnnualHeatGainKiloWattHours = null,
    IReadOnlyList<double>? ExteriorComparisonHeatFlowWatts = null,
    int? ExpectedColdestStepIndex = null,
    int? ExpectedWarmestStepIndex = null);

internal static class GroundBoundaryFoundationFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "foundation");

    public static IReadOnlyList<GroundBoundaryFoundationFixture> LoadAll()
    {
        if (!Directory.Exists(FixtureDirectory))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {FixtureDirectory}");

        var fixtures = Directory.GetFiles(FixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToArray();

        if (fixtures.Length == 0)
            throw new InvalidOperationException($"No fixture files were found in {FixtureDirectory}.");

        return fixtures;
    }

    private static GroundBoundaryFoundationFixture LoadFromFile(string path)
    {
        var fixture = JsonSerializer.Deserialize<GroundBoundaryFoundationFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {path}");

        return fixture;
    }
}
