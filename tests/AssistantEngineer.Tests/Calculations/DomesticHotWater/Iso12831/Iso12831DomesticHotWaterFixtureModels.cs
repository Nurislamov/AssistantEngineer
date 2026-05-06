using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

internal sealed record Iso12831DomesticHotWaterFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    Iso12831DomesticHotWaterInput Input,
    Iso12831DomesticHotWaterExpectedResult Expected,
    Iso12831DomesticHotWaterFixtureTolerance Tolerance);

internal sealed record Iso12831DomesticHotWaterExpectedResult(
    double DailyVolumeLiters,
    double DailyDrawEnergyKWh,
    double DailyTotalEnergyKWh,
    double AnnualVolumeLiters,
    double AnnualDrawEnergyKWh,
    double AnnualTotalEnergyKWh,
    double EquivalentOccupantsUsed,
    double ReferenceDailyVolumeLiters,
    int HourlyResultsCount);

internal sealed record Iso12831DomesticHotWaterFixtureTolerance(
    double Absolute,
    double RelativePercent);

internal static class Iso12831DomesticHotWaterFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831");

    public static IReadOnlyList<Iso12831DomesticHotWaterFixture> LoadAll()
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

    private static Iso12831DomesticHotWaterFixture LoadFromFile(string filePath)
    {
        var fixture = JsonSerializer.Deserialize<Iso12831DomesticHotWaterFixture>(
            File.ReadAllText(filePath),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {filePath}");

        return fixture;
    }
}
