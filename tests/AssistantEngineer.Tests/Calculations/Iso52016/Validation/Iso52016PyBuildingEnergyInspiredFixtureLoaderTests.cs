using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016PyBuildingEnergyInspiredFixtureLoaderTests
{
    private readonly Iso52016ExternalValidationFixtureLoader _loader = new();

    [Fact]
    public void PyBuildingEnergyInspiredFixtures_ParseWithRequiredReferenceMetadata()
    {
        var fixtures = _loader.LoadFromDirectory(FixtureDirectory());

        Assert.Equal(3, fixtures.Count);
        Assert.All(fixtures, fixture =>
        {
            Assert.Equal(Iso52016ExternalValidationFixtureSourceKind.PyBuildingEnergyInspiredNaming, fixture.SourceKind);
            Assert.NotNull(fixture.Reference);
            Assert.Equal("PyBuildingEnergyInspiredMethodologyNote", fixture.Reference!.DerivationKind);
            Assert.Contains("not a parity claim", fixture.Reference.SourceDescription, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string FixtureDirectory() =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "external-validation", "pybe-inspired");
}
