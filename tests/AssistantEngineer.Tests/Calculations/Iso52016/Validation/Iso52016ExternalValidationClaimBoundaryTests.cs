using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ExternalValidationClaimBoundaryTests
{
    private readonly Iso52016ExternalValidationFixtureLoader _loader = new();

    [Fact]
    public void AllFixtures_ContainRequiredNonClaims()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());

        Assert.All(fixtures, fixture =>
        {
            Assert.Contains("Validation/internal engineering anchors only.", fixture.ClaimBoundary);
            Assert.Contains("Manual independent reference fixtures only.", fixture.ClaimBoundary);
            Assert.Contains("No full ISO 52016 parity claim.", fixture.ClaimBoundary);
            Assert.Contains("No pyBuildingEnergy parity claim.", fixture.ClaimBoundary);
            Assert.Contains("No EnergyPlus parity claim.", fixture.ClaimBoundary);
            Assert.Contains("No ASHRAE 140 validation claim.", fixture.ClaimBoundary);
            Assert.Contains("ExternalParityCovered is not allowed in this stage.", fixture.ClaimBoundary);
        });
    }

    [Fact]
    public void Loader_RejectsExternalParityCoveredMarker()
    {
        var fixture = CreateValidFixtureObject();
        fixture["claimBoundary"] = new[]
        {
            "Validation/internal engineering anchors only.",
            "Manual independent reference fixtures only.",
            "No full ISO 52016 parity claim.",
            "No pyBuildingEnergy parity claim.",
            "No EnergyPlus parity claim.",
            "No ASHRAE 140 validation claim.",
            "ExternalParityCovered is not allowed in this stage.",
            "ExternalParityCovered"
        };

        var file = WriteTempFixture(fixture);
        var exception = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromFile(file));

        Assert.Contains("Forbidden positive claim", exception.Message);
    }

    [Fact]
    public void Loader_RejectsPositiveParityClaims()
    {
        var positiveToken = "pyBuildingEnergy" + " parity";
        var fixture = CreateValidFixtureObject();
        fixture["claimBoundary"] = new[]
        {
            "Validation/internal engineering anchors only.",
            "Manual independent reference fixtures only.",
            "No full ISO 52016 parity claim.",
            "No pyBuildingEnergy parity claim.",
            "No EnergyPlus parity claim.",
            "No ASHRAE 140 validation claim.",
            "ExternalParityCovered is not allowed in this stage.",
            "This text must be rejected: " + positiveToken
        };

        var file = WriteTempFixture(fixture);
        var exception = Assert.Throws<InvalidOperationException>(() => _loader.LoadFromFile(file));

        Assert.Contains("Forbidden positive claim", exception.Message);
    }

    private static string FixtureDirectory() =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "external-validation");

    private static Dictionary<string, object?> CreateValidFixtureObject() =>
        new(StringComparer.Ordinal)
        {
            ["id"] = "temp-claim-boundary",
            ["sourceKind"] = "ManualIndependent",
            ["calculationPath"] = "MatrixReduced",
            ["claimBoundary"] = new[]
            {
                "Validation/internal engineering anchors only.",
                "Manual independent reference fixtures only.",
                "No full ISO 52016 parity claim.",
                "No pyBuildingEnergy parity claim.",
                "No EnergyPlus parity claim.",
                "No ASHRAE 140 validation claim.",
                "ExternalParityCovered is not allowed in this stage."
            },
            ["reference"] = new Dictionary<string, object?>
            {
                ["derivationDocument"] = "docs/calculations/validation/iso52016/manual-independent-steady-heating-simple-room.md",
                ["derivationKind"] = "ManualIndependentArithmetic",
                ["sourceDescription"] = "Hand-calculated independent reference case for validation anchor only."
            },
            ["input"] = new Dictionary<string, object?>
            {
                ["sample"] = "value"
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
                ["relativeTolerancePercent"] = 1.0
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
