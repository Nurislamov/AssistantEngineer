using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class PeriodicIso52016GroundBoundaryTemperatureProviderTests
{
    private readonly PeriodicIso52016GroundBoundaryTemperatureProvider _provider = new();

    [Fact]
    public void BuildProfile_InOutdoorAirMode_ReturnsOutdoorTemperature()
    {
        var profile = CreateWeatherSolarProfile();

        var result = _provider.BuildProfile(
            new(
                WeatherSolarProfile: profile,
                Options: new(
                    Mode: Iso52016GroundBoundaryTemperatureMode.OutdoorAir)));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            profile.GetHour(0).Weather.DryBulbTemperatureC,
            result.Value.GetHour(0).GroundTemperatureC);

        Assert.Equal(
            profile.GetHour(2000).Weather.DryBulbTemperatureC,
            result.Value.GetHour(2000).GroundTemperatureC);
    }

    [Fact]
    public void BuildProfile_InFixedMode_ReturnsFixedTemperature()
    {
        var profile = CreateWeatherSolarProfile();

        var result = _provider.BuildProfile(
            new(
                WeatherSolarProfile: profile,
                Options: new(
                    Mode: Iso52016GroundBoundaryTemperatureMode.Fixed,
                    FixedGroundTemperatureC: 12.5)));

        Assert.True(result.IsSuccess);

        Assert.All(
            result.Value.Hours,
            hour => Assert.Equal(
                12.5,
                hour.GroundTemperatureC));
    }

    [Fact]
    public void BuildProfile_InPeriodicMode_ReturnsCompleteProfile()
    {
        var profile = CreateWeatherSolarProfile();

        var result = _provider.BuildProfile(
            new(
                WeatherSolarProfile: profile,
                Options: new(
                    Mode: Iso52016GroundBoundaryTemperatureMode.Periodic,
                    DepthM: 1.5)));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            profile.HourCount,
            result.Value.HourCount);

        Assert.Equal(
            0,
            result.Value.GetHour(0).HourOfYear);

        Assert.Equal(
            8759,
            result.Value.GetHour(8759).HourOfYear);
    }

    [Fact]
    public void BuildProfile_InPeriodicMode_DampsAnnualTemperatureSwing()
    {
        var profile = CreateWeatherSolarProfile();

        var result = _provider.BuildProfile(
            new(
                WeatherSolarProfile: profile,
                Options: new(
                    Mode: Iso52016GroundBoundaryTemperatureMode.Periodic,
                    DepthM: 2.0)));

        Assert.True(result.IsSuccess);

        var outdoorRange =
            profile.Hours.Max(hour => hour.Weather.DryBulbTemperatureC) -
            profile.Hours.Min(hour => hour.Weather.DryBulbTemperatureC);

        var groundRange =
            result.Value.Hours.Max(hour => hour.GroundTemperatureC) -
            result.Value.Hours.Min(hour => hour.GroundTemperatureC);

        Assert.True(
            groundRange < outdoorRange);
    }

    [Fact]
    public void BuildProfile_RejectsFixedModeWithoutFixedTemperature()
    {
        var profile = CreateWeatherSolarProfile();

        var result = _provider.BuildProfile(
            new(
                WeatherSolarProfile: profile,
                Options: new(
                    Mode: Iso52016GroundBoundaryTemperatureMode.Fixed)));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Fixed ground temperature is required when ground boundary mode is Fixed.",
            result.Error);
    }

    private static AnnualWeatherSolarProfile CreateWeatherSolarProfile()
    {
        var start = new DateTimeOffset(
            year: 2026,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.FromHours(5));

        var hours = Enumerable
            .Range(0, AnnualWeatherDataSet.NonLeapYearHourCount)
            .Select(hourOfYear =>
            {
                var timestamp = start.AddHours(hourOfYear);
                var seasonalTemperature = CalculateSeasonalTemperature(timestamp.DayOfYear);

                return new HourlyWeatherSolarRecord(
                    HourOfYear: hourOfYear,
                    Weather: new HourlyWeatherRecord(
                        HourOfYear: hourOfYear,
                        Timestamp: timestamp,
                        Month: timestamp.Month,
                        Day: timestamp.Day,
                        Hour: timestamp.Hour,
                        DryBulbTemperatureC: seasonalTemperature,
                        DirectNormalIrradianceWm2: 0,
                        DiffuseHorizontalIrradianceWm2: 0),
                    SolarPosition: new(
                        DayOfYear: timestamp.DayOfYear,
                        SolarDeclinationDegrees: 0,
                        EquationOfTimeMinutes: 0,
                        HourAngleDegrees: 0,
                        SolarAltitudeDegrees: 0,
                        SolarAzimuthDegrees: 180,
                        ZenithAngleDegrees: 90,
                        RelativeAirMass: 0),
                    SurfaceIrradiance: []);
            })
            .ToArray();

        return new AnnualWeatherSolarProfile(
            Year: 2026,
            TimeZoneOffset: TimeSpan.FromHours(5),
            LatitudeDegrees: 41.3,
            LongitudeDegrees: 69.2,
            Surfaces: [],
            Hours: hours);
    }

    private static double CalculateSeasonalTemperature(
        int dayOfYear)
    {
        const double mean = 15.0;
        const double amplitude = 20.0;

        return mean -
               amplitude *
               Math.Cos(
                   2.0 *
                   Math.PI *
                   (dayOfYear - 15) /
                   365.0);
    }
}