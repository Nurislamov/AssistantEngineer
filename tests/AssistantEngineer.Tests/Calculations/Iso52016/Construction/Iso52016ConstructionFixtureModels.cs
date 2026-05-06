using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Construction;

internal sealed record Iso52016ConstructionFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    Iso52016ConstructionAssembly Input,
    Iso52016ConstructionExpectedResult Expected,
    Iso52016ConstructionFixtureTolerance Tolerance);

internal sealed record Iso52016ConstructionExpectedResult(
    double TotalResistanceM2KPerW,
    double UValueWPerM2K,
    double ArealHeatCapacityJPerM2K,
    double EffectiveInternalHeatCapacityJPerM2K,
    Iso52016ConstructionMassClass MassClass,
    int NodeCount);

internal sealed record Iso52016ConstructionFixtureTolerance(
    double Absolute,
    double RelativePercent);

internal static class Iso52016ConstructionFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "construction");

    public static IReadOnlyList<Iso52016ConstructionFixture> LoadAll()
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

    private static Iso52016ConstructionFixture LoadFromFile(string path)
    {
        var fixture = JsonSerializer.Deserialize<Iso52016ConstructionFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {path}");

        return fixture;
    }
}
