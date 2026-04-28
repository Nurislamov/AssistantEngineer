using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016WindowSolarGainProfileBuilderTests
{
    private readonly Iso52016WindowSolarGainProfileBuilder _builder =
        new(
            new Iso52016WindowSolarGainCalculator());

    [Fact]
    public void Build_ReturnsHourlyProfile()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016WindowSolarGainProfileRequest(
                WeatherSolarContext: context,
                Orientation: CardinalDirection.South,
                WindowAreaM2: 2.0,
                SolarHeatGainCoefficient: 0.6));

        Assert.True(result.IsSuccess);

        Assert.Equal(24, result.Value.HourCount);
        Assert.Equal(600.0 * 24.0 / 1000.0, result.Value.AnnualSolarGainsKWh, precision: 6);

        var firstHour = result.Value.GetHour(0);

        Assert.Equal(0, firstHour.HourOfYear);
        Assert.Equal(CardinalDirection.South, firstHour.Orientation);
        Assert.Equal(WeatherSolarSurfaceCodes.South, firstHour.SurfaceCode);
        Assert.Equal(600.0, firstHour.SolarGainW, precision: 6);
    }

    [Fact]
    public void Build_AppliesFrameAndShadingAcrossProfile()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016WindowSolarGainProfileRequest(
                WeatherSolarContext: context,
                Orientation: CardinalDirection.South,
                WindowAreaM2: 2.0,
                SolarHeatGainCoefficient: 0.6,
                FrameFraction: 0.25,
                ShadingFactor: 0.5));

        Assert.True(result.IsSuccess);

        Assert.All(
            result.Value.Hours,
            hour => Assert.Equal(
                225.0,
                hour.SolarGainW,
                precision: 6));
    }

    [Fact]
    public void Build_RejectsEmptyContext()
    {
        var context = new Iso52016WeatherSolarContext(
            Year: 2026,
            TimeZoneOffset: TimeSpan.Zero,
            LatitudeDegrees: 0,
            LongitudeDegrees: 0,
            Hours: []);

        var result = _builder.Build(
            new Iso52016WindowSolarGainProfileRequest(
                WeatherSolarContext: context,
                Orientation: CardinalDirection.South,
                WindowAreaM2: 2.0,
                SolarHeatGainCoefficient: 0.6));

        Assert.True(result.IsFailure);
        Assert.Equal("ISO 52016 weather-solar context must contain hourly records.", result.Error);
    }

    private static Iso52016WeatherSolarContext CreateContext()
    {
        var hours = Enumerable
            .Range(0, 24)
            .Select(hour => new Iso52016HourlyWeatherSolarRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: 10,
                GroundBoundaryTemperatureC: 12,
                SolarAltitudeDegrees: 30,
                SolarAzimuthDegrees: 180,
                DirectNormalIrradianceWm2: 600,
                DiffuseHorizontalIrradianceWm2: 100,
                GlobalHorizontalIrradianceWm2: 400,
                SurfaceIrradiance:
                [
                    new Iso52016SurfaceWeatherSolarRecord(
                        SurfaceCode: WeatherSolarSurfaceCodes.South,
                        Orientation: WeatherSolarSurface.South.Orientation,
                        IncidenceAngleDegrees: 45,
                        BeamIrradianceWm2: 300,
                        DiffuseSkyIrradianceWm2: 150,
                        GroundReflectedIrradianceWm2: 50,
                        TotalIrradianceWm2: 500)
                ]))
            .ToArray();

        return new Iso52016WeatherSolarContext(
            Year: 2026,
            TimeZoneOffset: TimeSpan.Zero,
            LatitudeDegrees: 0,
            LongitudeDegrees: 0,
            Hours: hours);
    }
}