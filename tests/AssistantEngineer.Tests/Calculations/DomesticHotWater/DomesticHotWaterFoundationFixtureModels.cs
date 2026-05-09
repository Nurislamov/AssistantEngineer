using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

internal sealed record DomesticHotWaterFoundationFixture(
    string Id,
    string Scenario,
    DomesticHotWaterDemandDefinition DemandDefinition,
    DomesticHotWaterDrawOffProfileResolution Resolution,
    int NumberOfSteps,
    IReadOnlyList<double>? Schedule,
    DomesticHotWaterLossDefinition LossDefinition,
    DomesticHotWaterFoundationExpected Expected,
    IReadOnlyList<string>? ExpectedDiagnosticCodes = null);

internal sealed record DomesticHotWaterFoundationExpected(
    double? TotalVolumeLiters = null,
    double? TotalUsefulEnergyKWh = null,
    double? AnnualSystemLoadKWh = null,
    double? AnnualAuxiliaryEnergyKWh = null);

internal static class DomesticHotWaterFoundationFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "foundation");

    public static IReadOnlyList<DomesticHotWaterFoundationFixture> LoadAll()
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

    private static DomesticHotWaterFoundationFixture LoadFromFile(string path)
    {
        var fixture = JsonSerializer.Deserialize<DomesticHotWaterFoundationFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {path}");

        return fixture;
    }
}
