using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;

namespace AssistantEngineer.Tests.Calculations.Solar;

public class SolarPositionCalculatorTests
{
    private readonly SolarPositionCalculator _calculator = new();

    [Fact]
    public void Calculate_ReturnsHighSunAtEquatorNearEquinoxNoon()
    {
        var result = _calculator.Calculate(
            new SolarPositionRequest(
                Timestamp: new DateTimeOffset(
                    year: 2026,
                    month: 3,
                    day: 20,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                LatitudeDegrees: 0.0,
                LongitudeDegrees: 0.0));

        Assert.InRange(
            result.SolarAltitudeDegrees,
            85.0,
            90.0);

        Assert.InRange(
            result.ZenithAngleDegrees,
            0.0,
            5.0);

        Assert.True(
            result.RelativeAirMass > 0);
    }

    [Fact]
    public void Calculate_ReturnsLowSunAtNorthernWinterNoon()
    {
        var result = _calculator.Calculate(
            new SolarPositionRequest(
                Timestamp: new DateTimeOffset(
                    year: 2026,
                    month: 12,
                    day: 21,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                LatitudeDegrees: 50.0,
                LongitudeDegrees: 0.0));

        Assert.InRange(
            result.SolarAltitudeDegrees,
            10.0,
            25.0);

        Assert.InRange(
            result.SolarAzimuthDegrees,
            150.0,
            210.0);
    }

    [Fact]
    public void Calculate_ReturnsSunBelowHorizonAtMidnight()
    {
        var result = _calculator.Calculate(
            new SolarPositionRequest(
                Timestamp: new DateTimeOffset(
                    year: 2026,
                    month: 6,
                    day: 21,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                LatitudeDegrees: 40.0,
                LongitudeDegrees: 0.0));

        Assert.True(
            result.SolarAltitudeDegrees < 0);

        Assert.Equal(
            0.0,
            result.RelativeAirMass);
    }

    [Fact]
    public void Calculate_WithExplicitTashkentLocation_UsesLocalSolarTime()
    {
        var result = _calculator.Calculate(
            new SolarPositionRequest(
                Timestamp: new DateTimeOffset(
                    year: 2026,
                    month: 6,
                    day: 21,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(5)),
                Location: new SolarLocation(
                    LatitudeDegrees: 41.2995,
                    LongitudeDegrees: 69.2401,
                    UtcOffset: TimeSpan.FromHours(5))));

        Assert.InRange(
            result.HourAngleDegrees,
            -9.0,
            -4.0);

        Assert.True(
            result.SolarAltitudeDegrees > 70.0);
    }

    [Theory]
    [InlineData(-91.0, 0.0)]
    [InlineData(91.0, 0.0)]
    [InlineData(0.0, -181.0)]
    [InlineData(0.0, 181.0)]
    public void Calculate_RejectsInvalidCoordinates(
        double latitude,
        double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(
                new SolarPositionRequest(
                    Timestamp: DateTimeOffset.UtcNow,
                    LatitudeDegrees: latitude,
                    LongitudeDegrees: longitude)));
    }

    [Fact]
    public void Calculate_RejectsInvalidUtcOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(
                new SolarPositionRequest(
                    Timestamp: DateTimeOffset.UtcNow,
                    Location: new SolarLocation(
                        LatitudeDegrees: 41.3,
                        LongitudeDegrees: 69.2,
                        UtcOffset: TimeSpan.FromHours(15)))));
    }
}
