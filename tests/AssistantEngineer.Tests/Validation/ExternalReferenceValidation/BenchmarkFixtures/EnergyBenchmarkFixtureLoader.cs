using System.Text.Json;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.BenchmarkFixtures;

internal static class EnergyBenchmarkFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] RequiredProperties =
    [
        "fixtureName",
        "description",
        "category",
        "referenceType",
        "method",
        "status",
        "input",
        "expected",
        "tolerances",
        "assumptions",
        "notes"
    ];

    public static EnergyBenchmarkFixtureLoadResult LoadFromDefaultDirectory() =>
        LoadFromDirectory(DefaultFixtureDirectory());

    public static EnergyBenchmarkFixtureLoadResult LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Energy benchmark fixture directory was not found: {directory}");

        var fixtures = new List<EnergyBenchmarkFixture>();
        var skipped = new List<EnergyBenchmarkSkippedFixture>();

        foreach (var path in Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            var fixture = ReadFixture(path);

            if (fixture.Status is "Pending" or "Disabled")
            {
                skipped.Add(new EnergyBenchmarkSkippedFixture(
                    fixture.FixtureName,
                    path,
                    fixture.Status,
                    $"Fixture '{fixture.FixtureName}' has status {fixture.Status} and is skipped by default."));
                continue;
            }

            fixtures.Add(fixture);
        }

        return new EnergyBenchmarkFixtureLoadResult(fixtures, skipped);
    }

    private static EnergyBenchmarkFixture ReadFixture(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            ValidateRequiredProperties(root, path);

            var fixture = JsonSerializer.Deserialize<EnergyBenchmarkFixture>(json, JsonOptions) ??
                          throw new InvalidOperationException(
                              $"Energy benchmark fixture '{path}' could not be deserialized.");

            ValidateFixture(fixture, root, path);
            return fixture;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture JSON is invalid: {path}. {ex.Message}",
                ex);
        }
    }

    private static void ValidateRequiredProperties(JsonElement root, string path)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Energy benchmark fixture '{path}' must be a JSON object.");

        foreach (var propertyName in RequiredProperties)
        {
            if (!TryGetProperty(root, propertyName, out _))
            {
                throw new InvalidOperationException(
                    $"Energy benchmark fixture '{path}' is missing required field '{propertyName}'.");
            }
        }
    }

    private static void ValidateFixture(EnergyBenchmarkFixture fixture, JsonElement root, string path)
    {
        RequireText(fixture.FixtureName, "fixtureName", path);
        RequireText(fixture.Description, "description", path);
        RequireText(fixture.Category, "category", path);
        RequireText(fixture.ReferenceType, "referenceType", path);
        RequireText(fixture.Method, "method", path);
        RequireText(fixture.Status, "status", path);

        RequireAllowed(
            fixture.ReferenceType,
            "referenceType",
            EnergyBenchmarkFixtureMetadata.ReferenceTypes,
            fixture.FixtureName,
            path);
        RequireAllowed(
            fixture.Status,
            "status",
            EnergyBenchmarkFixtureMetadata.Statuses,
            fixture.FixtureName,
            path);
        RequireAllowed(
            fixture.Category,
            "category",
            EnergyBenchmarkFixtureMetadata.Categories,
            fixture.FixtureName,
            path);

        if (!TryGetProperty(root, "expected", out var expected) || expected.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{fixture.FixtureName}' field 'expected' must be a JSON object.");
        }

        if (!TryGetProperty(root, "tolerances", out var tolerances) || tolerances.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{fixture.FixtureName}' field 'tolerances' must be a JSON object.");
        }

        if (fixture.Tolerances.DefaultAbsolute is null &&
            fixture.Tolerances.DefaultRelativePercent is null &&
            fixture.Tolerances.Fields.Count == 0)
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{fixture.FixtureName}' must define at least one default or field tolerance.");
        }

        if (!TryGetProperty(root, "assumptions", out var assumptions) || assumptions.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{fixture.FixtureName}' field 'assumptions' must be an array.");
        }

        if (!TryGetProperty(root, "notes", out var notes) || notes.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{fixture.FixtureName}' field 'notes' must be an array.");
        }

        if (fixture.Input.HourlyRecordCount < 0)
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{fixture.FixtureName}' hourlyRecordCount must not be negative.");
        }
    }

    private static void RequireText(string value, string fieldName, string path)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{path}' field '{fieldName}' must not be empty.");
    }

    private static void RequireAllowed(
        string value,
        string fieldName,
        ISet<string> allowedValues,
        string fixtureName,
        string path)
    {
        if (!allowedValues.Contains(value))
        {
            throw new InvalidOperationException(
                $"Energy benchmark fixture '{fixtureName}' in '{path}' has invalid {fieldName} '{value}'. " +
                $"Allowed values: {string.Join(", ", allowedValues.Order(StringComparer.Ordinal))}.");
        }
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string DefaultFixtureDirectory() =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Validation",
            "ExternalReferenceValidation",
            "BenchmarkFixtures",
            "Fixtures");
}

internal sealed record EnergyBenchmarkFixtureLoadResult(
    IReadOnlyList<EnergyBenchmarkFixture> Fixtures,
    IReadOnlyList<EnergyBenchmarkSkippedFixture> SkippedFixtures);

internal sealed record EnergyBenchmarkSkippedFixture(
    string FixtureName,
    string Path,
    string Status,
    string Reason);

