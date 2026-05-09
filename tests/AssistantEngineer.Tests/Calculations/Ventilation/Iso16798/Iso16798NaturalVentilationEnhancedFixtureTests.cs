using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationEnhancedFixtureTests
{
    private readonly Iso16798NaturalVentilationCalculator _calculator = new();

    [Fact]
    public void EnhancedFixtures_ValidateBranchAndControlBehavior()
    {
        var fixtures = LoadAll();
        Assert.NotEmpty(fixtures);

        foreach (var fixture in fixtures)
        {
            Assert.Contains(
                fixture.ClaimBoundary,
                line => line.Contains("internal analytical anchor", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(
                fixture.ClaimBoundary,
                line => line.Contains("not full validation", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(
                fixture.ClaimBoundary,
                line => line.Contains("no full en16798 compliance claim", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(
                fixture.ClaimBoundary,
                line => line.Contains("no external validation claim", StringComparison.OrdinalIgnoreCase));

            var result = _calculator.Calculate(fixture.Input);

            Assert.Equal(fixture.Expected.SelectedBranch, result.SelectedBranch);

            if (fixture.Expected.ExpectZeroAirflow)
            {
                Assert.Equal(0.0, result.AirflowM3PerHour);
            }
            else
            {
                Assert.True(result.AirflowM3PerHour > 0.0);
            }

            if (fixture.Expected.ExpectPositiveWindComponent)
                Assert.True(result.WindComponentM3PerHour > 0.0);
            else
                Assert.True(result.WindComponentM3PerHour <= 0.0);

            if (fixture.Expected.ExpectPositiveStackComponent)
                Assert.True(result.StackComponentM3PerHour > 0.0);
            else
                Assert.True(result.StackComponentM3PerHour <= 0.0);

            if (fixture.Expected.ExpectClampedAch)
            {
                Assert.True(result.AirChangesPerHour > result.ClampedAirChangesPerHour);
                Assert.False(string.IsNullOrWhiteSpace(result.ClampReason));
            }
            else
            {
                Assert.True(result.AirChangesPerHour <= result.ClampedAirChangesPerHour || Math.Abs(result.AirChangesPerHour - result.ClampedAirChangesPerHour) < 1e-9);
            }

            if (fixture.Expected.ExpectClosedBySchedule)
                Assert.Contains("schedule", result.ControlReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            if (fixture.Expected.ExpectClosedByOccupancy)
                Assert.Contains("occupancy", result.ControlReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlyList<EnhancedFixtureDocument> LoadAll()
    {
        var directory = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ventilation", "natural");
        var files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
        return files
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => JsonSerializer.Deserialize<EnhancedFixtureDocument>(File.ReadAllText(path), SerializerOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize natural ventilation fixture '{path}'."))
            .ToArray();
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record EnhancedFixtureDocument(
        string Id,
        IReadOnlyList<string> ClaimBoundary,
        Iso16798NaturalVentilationInput Input,
        EnhancedFixtureExpected Expected);

    private sealed record EnhancedFixtureExpected(
        string SelectedBranch,
        bool ExpectZeroAirflow,
        bool ExpectPositiveWindComponent,
        bool ExpectPositiveStackComponent,
        bool ExpectClampedAch,
        bool ExpectClosedBySchedule,
        bool ExpectClosedByOccupancy);
}
