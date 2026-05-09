using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

internal sealed record SystemEnergyFoundationFixture(
    string Id,
    string Scenario,
    IReadOnlyList<SystemEnergyUsefulLoadInput> LoadInputs,
    IReadOnlyList<SystemEnergyStageDefinition> StageDefinitions,
    IReadOnlyList<SystemEnergyGeneratorDefinition> GeneratorDefinitions,
    EnergyFactorCatalog FactorCatalog,
    SystemEnergyLossOwnershipPolicy OwnershipPolicy,
    bool StrictFactorMode,
    SystemEnergyFoundationExpected Expected,
    IReadOnlyList<string>? ExpectedDiagnosticCodes = null);

internal sealed record SystemEnergyFoundationExpected(
    double? AnnualFinalEnergyKWh = null,
    double? AnnualPrimaryEnergyKWh = null,
    double? AnnualCo2Kg = null);

internal static class SystemEnergyFoundationFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "foundation");

    public static IReadOnlyList<SystemEnergyFoundationFixture> LoadAll()
    {
        if (!Directory.Exists(FixtureDirectory))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {FixtureDirectory}");

        var fixtures = Directory.GetFiles(FixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToArray();

        if (fixtures.Length == 0)
            throw new InvalidOperationException($"No fixture files were found in {FixtureDirectory}.");

        return fixtures;
    }

    private static SystemEnergyFoundationFixture LoadFromFile(string path)
    {
        var fixture = JsonSerializer.Deserialize<SystemEnergyFoundationFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {path}");

        return fixture;
    }
}
