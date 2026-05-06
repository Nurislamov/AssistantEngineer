using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationFixtureTests
{
    private readonly Iso16798NaturalVentilationCalculator _calculator = new();

    [Fact]
    public void AllFixtures_MatchExpectedResultsWithinTolerance()
    {
        var fixtures = Iso16798NaturalVentilationFixtureLoader.LoadAll();

        foreach (var fixture in fixtures)
        {
            var result = _calculator.Calculate(fixture.Input);
            Assert.Equal(Enum.Parse<Iso16798NaturalVentilationCalculationMode>(fixture.Expected.CalculationMode), result.CalculationMode);

            AssertWithinTolerance(fixture.Id, "EffectiveOpeningAreaM2", fixture.Expected.EffectiveOpeningAreaM2, result.EffectiveOpeningAreaM2, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "StackAirflowM3PerS", fixture.Expected.StackAirflowM3PerS, result.StackAirflowM3PerS, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "WindAirflowM3PerS", fixture.Expected.WindAirflowM3PerS, result.WindAirflowM3PerS, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "TotalAirflowM3PerS", fixture.Expected.TotalAirflowM3PerS, result.TotalAirflowM3PerS, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "TotalAirflowM3PerH", fixture.Expected.TotalAirflowM3PerH, result.TotalAirflowM3PerH, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "AirChangesPerHour", fixture.Expected.AirChangesPerHour, result.AirChangesPerHour, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "ClampedAirChangesPerHour", fixture.Expected.ClampedAirChangesPerHour, result.ClampedAirChangesPerHour, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "HeatTransferCoefficientWPerK", fixture.Expected.HeatTransferCoefficientWPerK, result.HeatTransferCoefficientWPerK, fixture.Tolerance);
        }
    }

    private static void AssertWithinTolerance(
        string fixtureId,
        string metricName,
        double expected,
        double actual,
        Iso16798NaturalVentilationFixtureTolerance tolerance)
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
