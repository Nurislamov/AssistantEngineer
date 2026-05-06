using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

internal sealed record Iso13370GroundBoundaryFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    Iso13370GroundBoundaryInput Input,
    Iso13370GroundBoundaryExpectedResult Expected,
    Iso13370GroundBoundaryFixtureTolerance Tolerance);

internal sealed record Iso13370GroundBoundaryExpectedResult(
    double CharacteristicDimensionM,
    double EquivalentGroundUValueWPerM2K,
    double HeatTransferCoefficientWPerK,
    double GroundWeight,
    double OutdoorWeight,
    double IndoorWeight,
    IReadOnlyList<double> MonthlyBoundaryTemperaturesC,
    double AnnualMeanBoundaryTemperatureC);

internal sealed record Iso13370GroundBoundaryFixtureTolerance(
    double Absolute,
    double RelativePercent);

internal static class Iso13370GroundBoundaryFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370");

    public static IReadOnlyList<Iso13370GroundBoundaryFixture> LoadAll()
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

    private static Iso13370GroundBoundaryFixture LoadFromFile(string filePath)
    {
        var fixture = JsonSerializer.Deserialize<Iso13370GroundBoundaryFixture>(
            File.ReadAllText(filePath),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {filePath}");

        return fixture;
    }
}
