using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ExternalValidationFixtureLoaderTests
{
    private readonly Iso52016ExternalValidationFixtureLoader _loader = new();

    [Fact]
    public void LoadFromDirectory_ParsesManualIndependentFixtures()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());

        Assert.Equal(3, fixtures.Count);
        Assert.All(fixtures, fixture =>
        {
            Assert.Equal(Iso52016ExternalValidationFixtureSourceKind.ManualIndependent, fixture.SourceKind);
            Assert.False(string.IsNullOrWhiteSpace(fixture.Id));
            Assert.NotNull(fixture.Expected);
            Assert.NotNull(fixture.Tolerance);
        });
    }

    [Fact]
    public void LoadFromFile_ThrowsWhenIdMissing()
    {
        var fixture = CreateValidFixtureObject();
        fixture.Remove("id");
        var file = WriteTempFixture(fixture);

        var exception = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromFile(file));

        Assert.Contains("Fixture id is required", exception.Message);
    }

    [Fact]
    public void LoadFromFile_ThrowsWhenManualClaimBoundaryLineMissing()
    {
        var fixture = CreateValidFixtureObject();
        fixture["claimBoundary"] = new[]
        {
            "Validation/internal engineering anchors only.",
            "No full ISO 52016 equivalence claim.",
            "No StandardReference equivalence claim.",
            "No EnergyPlus comparison workflow claim.",
            "No ASHRAE 140 / BESTEST-style validation anchor claim.",
            "ExternalReferenceCovered is not allowed in this stage."
        };

        var file = WriteTempFixture(fixture);
        var exception = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromFile(file));

        Assert.Contains("manual-only claim boundary line", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string FixtureDirectory() =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "external-validation");

    private static Dictionary<string, object?> CreateValidFixtureObject() =>
        new(StringComparer.Ordinal)
        {
            ["id"] = "temp-fixture",
            ["sourceKind"] = "ManualIndependent",
            ["calculationPath"] = "MatrixReduced",
            ["claimBoundary"] = new[]
            {
                "Validation/internal engineering anchors only.",
                "Manual independent reference fixtures only.",
                "No full ISO 52016 equivalence claim.",
                "No StandardReference equivalence claim.",
                "No EnergyPlus comparison workflow claim.",
                "No ASHRAE 140 / BESTEST-style validation anchor claim.",
                "ExternalReferenceCovered is not allowed in this stage."
            },
            ["reference"] = new Dictionary<string, object?>
            {
                ["derivationDocument"] = "docs/calculations/validation/iso52016/manual-independent-steady-heating-simple-room.md",
                ["derivationKind"] = "ManualIndependentArithmetic",
                ["sourceDescription"] = "Hand-calculated independent reference case for validation anchor only."
            },
            ["input"] = new Dictionary<string, object?>
            {
                ["outdoorTemperatureC"] = -2.0
            },
            ["expected"] = new Dictionary<string, object?>
            {
                ["annualHeatingKWh"] = 1000.0,
                ["hourlyResultCount"] = 24,
                ["monthlyHeatingKWh"] = Enumerable.Repeat(10.0, 12).ToArray(),
                ["monthlyCoolingKWh"] = Enumerable.Repeat(0.0, 12).ToArray()
            },
            ["tolerance"] = new Dictionary<string, object?>
            {
                ["absoluteTolerance"] = 1.0,
                ["relativeTolerancePercent"] = 1.5
            }
        };

    private static string WriteTempFixture(Dictionary<string, object?> fixtureObject)
    {
        var file = Path.Combine(Path.GetTempPath(), $"iso52016-ext-validation-{Guid.NewGuid():N}.json");
        var json = JsonSerializer.Serialize(fixtureObject);
        File.WriteAllText(file, json);
        return file;
    }
}
