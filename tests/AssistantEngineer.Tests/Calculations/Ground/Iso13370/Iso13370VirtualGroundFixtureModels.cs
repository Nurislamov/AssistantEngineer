using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

internal sealed record Iso13370VirtualGroundFixture(
    string Id,
    IReadOnlyList<string> ClaimBoundary,
    Iso13370VirtualGroundInput Input);

internal static class Iso13370VirtualGroundFixtureLoader
{
    private static readonly HashSet<string> FixtureNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "slab-on-ground-basic.json",
        "insulated-slab.json",
        "high-conductivity-ground.json",
        "thermal-bridge-enabled.json"
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370");

    public static IReadOnlyList<Iso13370VirtualGroundFixture> LoadAll()
    {
        if (!Directory.Exists(FixtureDirectory))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {FixtureDirectory}");

        var fixtures = Directory.GetFiles(FixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => FixtureNames.Contains(Path.GetFileName(path)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToArray();

        if (fixtures.Length != FixtureNames.Count)
            throw new InvalidOperationException($"Expected {FixtureNames.Count} virtual-ground fixtures in {FixtureDirectory}, but found {fixtures.Length}.");

        return fixtures;
    }

    private static Iso13370VirtualGroundFixture LoadFromFile(string filePath)
    {
        var fixture = JsonSerializer.Deserialize<Iso13370VirtualGroundFixture>(
            File.ReadAllText(filePath),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {filePath}");

        return fixture;
    }
}
