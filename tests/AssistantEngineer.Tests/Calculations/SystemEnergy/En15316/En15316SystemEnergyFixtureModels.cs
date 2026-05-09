using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

internal sealed record En15316SystemEnergyFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    En15316SystemEnergyInput Input,
    En15316SystemEnergyExpectedResult Expected,
    En15316SystemEnergyFixtureTolerance Tolerance);

internal sealed record En15316SystemEnergyExpectedResult(
    double TotalFinalEnergyKWh,
    double TotalPrimaryEnergyKWh,
    IReadOnlyDictionary<string, double> FinalByCarrier,
    IReadOnlyDictionary<string, double> PrimaryByCarrier);

internal sealed record En15316SystemEnergyFixtureTolerance(
    double Absolute,
    double RelativePercent);

internal static class En15316SystemEnergyFixtureLoader
{
    private static readonly HashSet<string> LegacyFixtureFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "boiler-heating-emission-distribution-generation.json",
        "condensing-boiler-heating-with-recovered-losses.json",
        "heat-pump-heating-electricity-primary.json",
        "chiller-cooling-electricity-primary.json",
        "dhw-storage-distribution-generation-chain.json"
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316");

    public static IReadOnlyList<En15316SystemEnergyFixture> LoadAll()
    {
        if (!Directory.Exists(FixtureDirectory))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {FixtureDirectory}");

        var fixtures = Directory
            .GetFiles(FixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => LegacyFixtureFileNames.Contains(Path.GetFileName(path)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToArray();

        if (fixtures.Length == 0)
            throw new InvalidOperationException($"No fixture files were found in {FixtureDirectory}.");

        return fixtures;
    }

    private static En15316SystemEnergyFixture LoadFromFile(string path)
    {
        var fixture = JsonSerializer.Deserialize<En15316SystemEnergyFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {path}");

        return fixture;
    }
}
