using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316SystemEnergyFixtureTests
{
    private readonly En15316SystemEnergyChainCalculator _calculator = new(
        new En15316SystemEnergyReferenceDataProvider());

    [Fact]
    public void AllFixtures_MatchExpectedResultsWithinTolerance()
    {
        var fixtures = En15316SystemEnergyFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var result = _calculator.Calculate(fixture.Input);
            Assert.True(result.IsSuccess, $"Fixture '{fixture.Id}' failed: {result.Error}");

            AssertWithinTolerance(
                fixture.Id,
                "TotalFinalEnergyKWh",
                fixture.Expected.TotalFinalEnergyKWh,
                result.Value.TotalFinalEnergyKWh,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "TotalPrimaryEnergyKWh",
                fixture.Expected.TotalPrimaryEnergyKWh,
                result.Value.TotalPrimaryEnergyKWh,
                fixture.Tolerance);

            foreach (var expectedCarrier in fixture.Expected.FinalByCarrier)
            {
                var carrier = Enum.Parse<En15316EnergyCarrier>(expectedCarrier.Key, ignoreCase: false);
                Assert.True(
                    result.Value.FinalEnergyByCarrierKWh.TryGetValue(carrier, out var actualValue),
                    $"Fixture '{fixture.Id}' is missing final carrier '{expectedCarrier.Key}'.");

                AssertWithinTolerance(
                    fixture.Id,
                    $"FinalEnergyByCarrier[{expectedCarrier.Key}]",
                    expectedCarrier.Value,
                    actualValue,
                    fixture.Tolerance);
            }

            foreach (var expectedCarrier in fixture.Expected.PrimaryByCarrier)
            {
                var carrier = Enum.Parse<En15316EnergyCarrier>(expectedCarrier.Key, ignoreCase: false);
                Assert.True(
                    result.Value.PrimaryEnergyByCarrierKWh.TryGetValue(carrier, out var actualValue),
                    $"Fixture '{fixture.Id}' is missing primary carrier '{expectedCarrier.Key}'.");

                AssertWithinTolerance(
                    fixture.Id,
                    $"PrimaryEnergyByCarrier[{expectedCarrier.Key}]",
                    expectedCarrier.Value,
                    actualValue,
                    fixture.Tolerance);
            }
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
