using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370GroundBoundaryFixtureTests
{
    private readonly Iso13370GroundBoundaryCalculator _calculator = new(new Iso13370GroundTemperatureProfileCalculator());

    [Fact]
    public void AllFixtures_MatchExpectedResultsWithinTolerance()
    {
        var fixtures = Iso13370GroundBoundaryFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var result = _calculator.Calculate(fixture.Input);

            AssertWithinTolerance(
                fixture.Id,
                "CharacteristicDimensionM",
                fixture.Expected.CharacteristicDimensionM,
                result.CharacteristicDimensionM,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "EquivalentGroundUValueWPerM2K",
                fixture.Expected.EquivalentGroundUValueWPerM2K,
                result.EquivalentGroundUValueWPerM2K,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "HeatTransferCoefficientWPerK",
                fixture.Expected.HeatTransferCoefficientWPerK,
                result.HeatTransferCoefficientWPerK,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "GroundWeight",
                fixture.Expected.GroundWeight,
                result.GroundWeight,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "OutdoorWeight",
                fixture.Expected.OutdoorWeight,
                result.OutdoorWeight,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "IndoorWeight",
                fixture.Expected.IndoorWeight,
                result.IndoorWeight,
                fixture.Tolerance);
            AssertWithinTolerance(
                fixture.Id,
                "AnnualMeanBoundaryTemperatureC",
                fixture.Expected.AnnualMeanBoundaryTemperatureC,
                result.AnnualMeanBoundaryTemperatureC,
                fixture.Tolerance);

            Assert.Equal(12, result.MonthlyBoundaryTemperaturesC.Count);
            Assert.Equal(12, fixture.Expected.MonthlyBoundaryTemperaturesC.Count);

            for (var month = 0; month < 12; month++)
            {
                AssertWithinTolerance(
                    fixture.Id,
                    $"MonthlyBoundaryTemperaturesC[{month}]",
                    fixture.Expected.MonthlyBoundaryTemperaturesC[month],
                    result.MonthlyBoundaryTemperaturesC[month],
                    fixture.Tolerance);
            }
        }
    }

    private static void AssertWithinTolerance(
        string fixtureId,
        string metricName,
        double expected,
        double actual,
        Iso13370GroundBoundaryFixtureTolerance tolerance)
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
