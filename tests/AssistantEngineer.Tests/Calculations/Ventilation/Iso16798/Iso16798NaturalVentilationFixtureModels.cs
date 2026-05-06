using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

internal sealed record Iso16798NaturalVentilationFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    Iso16798NaturalVentilationInput Input,
    Iso16798NaturalVentilationExpectedResult Expected,
    Iso16798NaturalVentilationFixtureTolerance Tolerance);

internal sealed record Iso16798NaturalVentilationExpectedResult(
    string CalculationMode,
    double EffectiveOpeningAreaM2,
    double StackAirflowM3PerS,
    double WindAirflowM3PerS,
    double TotalAirflowM3PerS,
    double TotalAirflowM3PerH,
    double AirChangesPerHour,
    double ClampedAirChangesPerHour,
    double HeatTransferCoefficientWPerK);

internal sealed record Iso16798NaturalVentilationFixtureTolerance(
    double Absolute,
    double RelativePercent);

internal static class Iso16798NaturalVentilationFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ventilation", "iso16798-natural");

    public static IReadOnlyList<Iso16798NaturalVentilationFixture> LoadAll()
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

    public static Iso16798NaturalVentilationFixture LoadById(string id) =>
        LoadAll().Single(fixture => fixture.Id == id);

    private static Iso16798NaturalVentilationFixture LoadFromFile(string filePath)
    {
        var fixture = JsonSerializer.Deserialize<Iso16798NaturalVentilationFixture>(
            File.ReadAllText(filePath),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {filePath}");

        return fixture;
    }
}
