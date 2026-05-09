using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316HeatingSystemCircuitFixtureTests
{
    private readonly En15316HeatingSystemCircuitCalculator _calculator = new(
        new En15316HeatingSystemInputValidator(),
        new En15316SystemEnergyReferenceDataProvider());

    [Fact]
    public void AllFixtures_MatchExpectedResultsWithinTolerance()
    {
        var fixtures = En15316HeatingSystemCircuitFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var result = _calculator.Calculate(fixture.Input);
            Assert.True(result.IsSuccess, $"Fixture '{fixture.Id}' failed: {result.Error}");

            AssertWithinTolerance(fixture.Id, "AnnualUsefulEnergyKWh", fixture.Expected.AnnualUsefulEnergyKWh, result.Value.AnnualUsefulEnergyKWh, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "AnnualFinalEnergyKWh", fixture.Expected.AnnualFinalEnergyKWh, result.Value.AnnualFinalEnergyKWh, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "AnnualPrimaryEnergyKWh", fixture.Expected.AnnualPrimaryEnergyKWh, result.Value.AnnualPrimaryEnergyKWh, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "AnnualDistributionLossEnergyKWh", fixture.Expected.AnnualDistributionLossEnergyKWh, result.Value.AnnualDistributionLossEnergyKWh, fixture.Tolerance);
            Assert.Equal(fixture.Expected.TimeStepCount, result.Value.TimeSteps.Count);
        }
    }

    private static void AssertWithinTolerance(
        string fixtureId,
        string metricName,
        double expected,
        double actual,
        En15316SystemEnergyFixtureTolerance tolerance)
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
