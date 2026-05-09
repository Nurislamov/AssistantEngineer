using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneInvalidTopologyFixtureTests
{
    [Fact]
    public void InvalidTopologyFixture_IsRejectedWithDeterministicDiagnostics()
    {
        var fixture = LoadFixture("invalid-topology-adjacent-boundary.json");
        var validator = new Iso52016MultiZoneInputValidator();

        var first = validator.Validate(fixture.Input);
        var second = validator.Validate(fixture.Input);

        Assert.False(first.IsValid);
        Assert.Equal(
            first.Diagnostics.Select(item => item.Code),
            second.Diagnostics.Select(item => item.Code));

        Assert.Contains(first.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.ExteriorBoundaryTargetZoneForbidden");
        Assert.Contains(first.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.InterZoneBoundarySelfLink");
        Assert.Contains(first.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.BoundaryAreaNonPositive");
        Assert.Contains(first.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.BoundaryConductanceNonPositive");
        Assert.Contains(first.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.InterZoneBoundaryDuplicatePair");
    }

    private static MultiZoneFixtureDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "multi-zone-invalid", fileName);
        var json = File.ReadAllText(path);
        var fixture = JsonSerializer.Deserialize<MultiZoneFixtureDocument>(json, JsonOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Failed to load fixture '{path}'.");

        return fixture;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record MultiZoneFixtureDocument(
        string Id,
        IReadOnlyList<string> ClaimBoundary,
        string? Notes,
        MultiZoneCalculationInput Input);
}
