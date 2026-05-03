using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;

namespace AssistantEngineer.Tests.Calculations.Solar;

public class SolarTimeCalculatorTests
{
    private readonly SolarTimeCalculator _calculator = new();

    [Fact]
    public void Calculate_GreenwichNearEquinoxNoon_ProducesNearSolarNoon()
    {
        var result = _calculator.Calculate(
            new DateTimeOffset(
                year: 2026,
                month: 3,
                day: 20,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero),
            new SolarLocation(
                LatitudeDegrees: 0,
                LongitudeDegrees: 0,
                UtcOffset: TimeSpan.Zero));

        Assert.Equal(79, result.DayOfYear);
        Assert.InRange(result.LocalSolarTimeHours, 11.75, 12.25);
        Assert.InRange(result.HourAngleDegrees, -4.0, 4.0);
    }

    [Fact]
    public void Calculate_TashkentUtcPlusFive_AppliesLongitudeCorrection()
    {
        var result = _calculator.Calculate(
            new DateTimeOffset(
                year: 2026,
                month: 6,
                day: 21,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.FromHours(5)),
            new SolarLocation(
                LatitudeDegrees: 41.2995,
                LongitudeDegrees: 69.2401,
                UtcOffset: TimeSpan.FromHours(5)));

        Assert.InRange(result.LongitudeCorrectionMinutes, -23.1, -22.9);
        Assert.InRange(result.LocalSolarTimeHours, 11.4, 11.7);
        Assert.InRange(result.HourAngleDegrees, -9.0, -4.0);
    }

    [Theory]
    [InlineData(-91.0, 69.2, 5)]
    [InlineData(91.0, 69.2, 5)]
    [InlineData(41.3, -181.0, 5)]
    [InlineData(41.3, 181.0, 5)]
    [InlineData(41.3, 69.2, -15)]
    [InlineData(41.3, 69.2, 15)]
    public void Calculate_RejectsInvalidLocation(
        double latitude,
        double longitude,
        int utcOffsetHours)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(
                DateTimeOffset.UtcNow,
                new SolarLocation(
                    LatitudeDegrees: latitude,
                    LongitudeDegrees: longitude,
                    UtcOffset: TimeSpan.FromHours(utcOffsetHours))));
    }
}
