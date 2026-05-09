using System.Text.Json;

namespace AssistantEngineer.Tests.Reporting;

internal sealed record EngineeringReportFixture(
    string Id,
    string ReportKind,
    IReadOnlyList<string> ExpectedSections,
    IReadOnlyList<string>? ExpectedHeadings = null,
    IReadOnlyList<string>? ExpectedTableColumns = null);

internal static class EngineeringReportFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string FixtureDirectory =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "reporting");

    public static EngineeringReportFixture Load(
        string fileName)
    {
        var path = Path.Combine(FixtureDirectory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Engineering report fixture file was not found: {path}", path);

        var fixture = JsonSerializer.Deserialize<EngineeringReportFixture>(
            File.ReadAllText(path),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture was not parsed: {path}");

        return fixture;
    }
}

