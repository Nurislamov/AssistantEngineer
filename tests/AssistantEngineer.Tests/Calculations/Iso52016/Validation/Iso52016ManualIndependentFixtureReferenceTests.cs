using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ManualIndependentFixtureReferenceTests
{
    private readonly Iso52016ExternalValidationFixtureLoader _loader = new();

    [Fact]
    public void EveryManualIndependentFixture_HasReferenceMetadataAndDerivationDocument()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());

        Assert.All(fixtures, fixture =>
        {
            Assert.Equal(Iso52016ExternalValidationFixtureSourceKind.ManualIndependent, fixture.SourceKind);
            Assert.NotNull(fixture.Reference);
            Assert.False(string.IsNullOrWhiteSpace(fixture.Reference!.DerivationDocument));
            Assert.Equal("ManualIndependentArithmetic", fixture.Reference.DerivationKind);

            var docPath = Path.Combine(
                TestPaths.RepoRoot,
                fixture.Reference.DerivationDocument.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(docPath), $"Derivation document was not found for fixture {fixture.Id}: {docPath}");
        });
    }

    [Fact]
    public void ManualIndependentReferences_DoNotDeclareParityClaims()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());
        var forbidden = new[] { "ASHRAE 140 validated", "validated against pyBuildingEnergy", "validated against EnergyPlus" };
        var boundaryTokens = new[]
        {
            "full ISO52016" + " parity",
            "full ISO 52016" + " parity",
            "pyBuildingEnergy" + " parity",
            "EnergyPlus" + " parity",
            "ExternalParityCovered"
        };

        foreach (var fixture in fixtures)
        {
            var text = string.Join(
                Environment.NewLine,
                fixture.ClaimBoundary
                    .Append(fixture.Reference!.SourceDescription)
                    .Append(fixture.Reference.DerivationDocument));

            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, text, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var line in fixture.ClaimBoundary)
            {
                foreach (var token in boundaryTokens)
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
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "external-validation");
}
