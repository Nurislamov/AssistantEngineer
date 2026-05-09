using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

internal sealed record En15316HeatingSystemCircuitFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    En15316HeatingSystemInput Input,
    En15316HeatingSystemCircuitExpectedResult Expected,
    En15316SystemEnergyFixtureTolerance Tolerance);

internal sealed record En15316HeatingSystemCircuitExpectedResult(
    double AnnualUsefulEnergyKWh,
    double AnnualFinalEnergyKWh,
    double AnnualPrimaryEnergyKWh,
    double AnnualDistributionLossEnergyKWh,
    int TimeStepCount);

internal static class En15316HeatingSystemCircuitFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316");

    private static readonly HashSet<string> FixtureFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "boiler-simple-circuit.json",
        "heat-pump-simple-circuit.json",
        "distribution-losses-enabled.json",
        "zero-demand.json"
    };

    public static IReadOnlyList<En15316HeatingSystemCircuitFixture> LoadAll()
    {
        if (!Directory.Exists(FixtureDirectory))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {FixtureDirectory}");

        var fixtures = Directory.GetFiles(FixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => FixtureFileNames.Contains(Path.GetFileName(path)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToArray();

        if (fixtures.Length == 0)
            throw new InvalidOperationException($"No circuit fixture files were found in {FixtureDirectory}.");

        return fixtures;
    }

    private static En15316HeatingSystemCircuitFixture LoadFromFile(string path)
    {
        var fixture = JsonSerializer.Deserialize<En15316HeatingSystemCircuitFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {path}");

        return fixture;
    }
}
