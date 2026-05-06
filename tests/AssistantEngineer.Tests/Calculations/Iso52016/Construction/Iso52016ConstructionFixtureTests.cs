using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Construction;

public sealed class Iso52016ConstructionFixtureTests
{
    private readonly Iso52016ConstructionAssemblyCalculator _calculator = new(new Iso52016ConstructionReferenceDataProvider());

    [Fact]
    public void AllFixtures_MatchExpectedResultsWithinTolerance()
    {
        var fixtures = Iso52016ConstructionFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var result = _calculator.Calculate(fixture.Input);

            AssertWithinTolerance(
                fixture.Id,
                "TotalResistanceM2KPerW",
                fixture.Expected.TotalResistanceM2KPerW,
                result.TotalResistanceM2KPerW,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "UValueWPerM2K",
                fixture.Expected.UValueWPerM2K,
                result.UValueWPerM2K,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "ArealHeatCapacityJPerM2K",
                fixture.Expected.ArealHeatCapacityJPerM2K,
                result.ArealHeatCapacityJPerM2K,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "EffectiveInternalHeatCapacityJPerM2K",
                fixture.Expected.EffectiveInternalHeatCapacityJPerM2K,
                result.EffectiveInternalHeatCapacityJPerM2K,
                fixture.Tolerance);

            Assert.Equal(fixture.Expected.MassClass, result.MassClass);
            Assert.Equal(fixture.Expected.NodeCount, result.Nodes.Count);
        }
    }

    private static void AssertWithinTolerance(
        string fixtureId,
        string metricName,
        double expected,
        double actual,
        Iso52016ConstructionFixtureTolerance tolerance)
    {
        var absoluteDelta = Math.Abs(expected - actual);
        var relativeDeltaPercent = Math.Abs(expected) > 1e-12
            ? absoluteDelta / Math.Abs(expected) * 100.0
            : absoluteDelta == 0.0 ? 0.0 : double.PositiveInfinity;

        var passes = absoluteDelta <= tolerance.Absolute ||
                     relativeDeltaPercent <= tolerance.RelativePercent;

        Assert.True(
            passes,
            $"Fixture '{fixtureId}' metric '{metricName}' is out of tolerance. Expected={expected}, Actual={actual}, |Delta|={absoluteDelta}, Relative%={relativeDeltaPercent}, AbsTol={tolerance.Absolute}, RelTol%={tolerance.RelativePercent}.");
    }
}
