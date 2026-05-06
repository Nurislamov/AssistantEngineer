using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016PyBuildingEnergyInspiredClaimBoundaryTests
{
    private readonly Iso52016ExternalValidationFixtureLoader _loader = new();

    [Fact]
    public void Fixtures_ContainRequiredPyBuildingEnergyInspiredNonClaims()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());

        foreach (var fixture in fixtures)
        {
            Assert.Contains("Validation/internal engineering anchors only.", fixture.ClaimBoundary);
            Assert.Contains("pyBuildingEnergy-inspired methodology alignment lane only.", fixture.ClaimBoundary);
            Assert.Contains("No pyBuildingEnergy parity claim.", fixture.ClaimBoundary);
            Assert.Contains("No pyBuildingEnergy numerical equivalence claim.", fixture.ClaimBoundary);
            Assert.Contains("No copied pyBuildingEnergy code.", fixture.ClaimBoundary);
            Assert.Contains("No pyBuildingEnergy runtime dependency.", fixture.ClaimBoundary);
            Assert.Contains("No full ISO 52016 parity claim.", fixture.ClaimBoundary);
            Assert.Contains("No EnergyPlus parity claim.", fixture.ClaimBoundary);
            Assert.Contains("No ASHRAE 140 validation claim.", fixture.ClaimBoundary);
            Assert.Contains("ExternalParityCovered is not allowed in this stage.", fixture.ClaimBoundary);
        }
    }

    [Fact]
    public void Fixtures_DoNotContainForbiddenPositiveClaims()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());
        var forbiddenTokens = new[]
        {
            "validated against " + "pyBuildingEnergy",
            "matches " + "pyBuildingEnergy",
            "same as " + "pyBuildingEnergy",
            "copied from " + "pyBuildingEnergy",
            "ASHRAE 140 " + "validated"
        };
        var boundaryTokensThatMustBeNegated = new[]
        {
            "full ISO52016" + " parity",
            "pyBuildingEnergy" + " parity",
            "EnergyPlus" + " parity"
        };

        foreach (var fixture in fixtures)
        {
            var text = string.Join(
                Environment.NewLine,
                fixture.ClaimBoundary
                    .Append(fixture.Reference?.SourceDescription ?? string.Empty)
                    .Append(fixture.Reference?.MethodologySourceName ?? string.Empty)
                    .Concat(fixture.Reference?.MethodologyNotes ?? Array.Empty<string>()));

            foreach (var token in forbiddenTokens)
                Assert.DoesNotContain(token, text, StringComparison.OrdinalIgnoreCase);

            foreach (var line in fixture.ClaimBoundary)
            {
                foreach (var token in boundaryTokensThatMustBeNegated)
                {
                    if (!line.Contains(token, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Assert.True(
                        line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("not allowed", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("must not", StringComparison.OrdinalIgnoreCase),
                        $"Claim-boundary token '{token}' must be negated in fixture {fixture.Id}: {line}");
                }
            }
        }
    }

    private static string FixtureDirectory() =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "external-validation", "pybe-inspired");
}
