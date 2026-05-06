using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Construction;

public sealed class Iso52016ConstructionMassClassTests
{
    private readonly Iso52016ConstructionReferenceDataProvider _provider = new();

    [Theory]
    [InlineData(10_000.0, Iso52016ConstructionMassClass.VeryLight)]
    [InlineData(80_000.0, Iso52016ConstructionMassClass.Light)]
    [InlineData(140_000.0, Iso52016ConstructionMassClass.Medium)]
    [InlineData(220_000.0, Iso52016ConstructionMassClass.Heavy)]
    [InlineData(320_000.0, Iso52016ConstructionMassClass.VeryHeavy)]
    public void ReferenceDataProvider_ResolvesMassClassByEffectiveCapacity(
        double effectiveCapacity,
        Iso52016ConstructionMassClass expected)
    {
        var actual = _provider.ResolveMassClass(effectiveCapacity);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FixtureMassClasses_AreStable()
    {
        var calculator = new Iso52016ConstructionAssemblyCalculator(_provider);
        var fixtures = Iso52016ConstructionFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var result = calculator.Calculate(fixture.Input);
            Assert.Equal(fixture.Expected.MassClass, result.MassClass);
        }
    }
}
