using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016StandardReferenceInspiredFixtureLoaderTests
{
    private readonly Iso52016ExternalValidationFixtureLoader _loader = new();

    [Fact]
    public void StandardReferenceInspiredFixtures_ParseWithRequiredReferenceMetadata()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());

        Assert.Equal(3, fixtures.Count);
        Assert.All(fixtures, fixture =>
        {
            Assert.Equal(Iso52016ExternalValidationFixtureSourceKind.StandardReferenceInspiredNaming, fixture.SourceKind);
            Assert.NotNull(fixture.Reference);
            Assert.Equal("StandardReferenceInspiredMethodologyNote", fixture.Reference!.DerivationKind);
            Assert.Contains("not a equivalence claim", fixture.Reference.SourceDescription, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string FixtureDirectory() =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "external-validation", "standard-reference-inspired");
}

