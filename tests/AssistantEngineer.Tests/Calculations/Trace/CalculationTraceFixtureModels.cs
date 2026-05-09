using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Trace;

internal sealed record CalculationTraceFixture(
    string Id,
    IReadOnlyList<string> ExpectedModules,
    IReadOnlyList<string> ExpectedStepNames,
    IReadOnlyList<string> ExpectedValueKeys,
    string? DetailLevel = null,
    int? MaxCollectionItems = null,
    bool? ExpectsCompactArraySummary = null);

internal static class CalculationTraceFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "calculation-trace");

    public static CalculationTraceFixture Load(
        string fileName)
    {
        var path = Path.Combine(FixtureDirectory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Calculation trace fixture file was not found: {path}", path);

        var fixture = JsonSerializer.Deserialize<CalculationTraceFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture was not parsed: {path}");

        return fixture;
    }
}
