using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;

namespace AssistantEngineer.Tests.Calculations.WeatherSolar;

public class AnnualWeatherSolarProfileBuilderTests
{
    private readonly AnnualWeatherSolarProfileBuilder _builder =
        new(
            new SolarPositionCalculator(),
            new IsotropicSkySurfaceIrradianceCalculator());

    [Fact]
    public void Build_CreatesProfileForCompleteYear()
    {
        var weather = CreateWeatherDataSet();

        var result = _builder.Build(
            new AnnualWeatherSolarProfileRequest(
                WeatherDataSet: weather,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2));

        Assert.True(result.IsSuccess);

        var profile = result.Value;

        Assert.Equal(weather.Year, profile.Year);
        Assert.Equal(weather.HourCount, profile.HourCount);
        Assert.Equal(9, profile.Surfaces.Count);

        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.Horizontal);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.North);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.NorthEast);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.East);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.SouthEast);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.South);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.SouthWest);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.West);
        Assert.Contains(profile.Surfaces, surface => surface.Code == WeatherSolarSurfaceCodes.NorthWest);
    }

    [Fact]
    public void Build_CreatesSurfaceIrradianceForEachHour()
    {
        var weather = CreateWeatherDataSet();

        var result = _builder.Build(
            new AnnualWeatherSolarProfileRequest(
                WeatherDataSet: weather,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2));

        Assert.True(result.IsSuccess);

        var noon = result.Value.GetHour(12);

        Assert.Equal(12, noon.HourOfYear);
        Assert.Equal(9, noon.SurfaceIrradiance.Count);

        var horizontal = noon.GetSurface(WeatherSolarSurfaceCodes.Horizontal);

        Assert.True(horizontal.Irradiance.TotalIrradianceWm2 >= 0);
    }

    [Fact]
    public void Build_AllowsCustomSurfaces()
    {
        var weather = CreateWeatherDataSet();

        var result = _builder.Build(
            new AnnualWeatherSolarProfileRequest(
                WeatherDataSet: weather,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                Surfaces:
                [
                    new WeatherSolarSurface(
                        "south-window",
                        new(
                            TiltDegrees: 90,
                            AzimuthDegrees: 180))
                ]));

        Assert.True(result.IsSuccess);

        var profile = result.Value;

        Assert.Single(profile.Surfaces);
        Assert.Equal("south-window", profile.Surfaces[0].Code);

        var noon = profile.GetHour(12);

        Assert.Single(noon.SurfaceIrradiance);
        Assert.Equal("south-window", noon.SurfaceIrradiance[0].SurfaceCode);
    }

    [Fact]
    public void Build_RejectsDuplicateSurfaceCodes()
    {
        var weather = CreateWeatherDataSet();

        var result = _builder.Build(
            new AnnualWeatherSolarProfileRequest(
                WeatherDataSet: weather,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                Surfaces:
                [
                    new WeatherSolarSurface("south", new(90, 180)),
                    new WeatherSolarSurface("south", new(90, 180))
                ]));

        Assert.True(result.IsFailure);
        Assert.Contains("Surface codes must be unique", result.Error);
    }

    [Theory]
    [InlineData(-91.0, 69.2)]
    [InlineData(91.0, 69.2)]
    [InlineData(41.3, -181.0)]
    [InlineData(41.3, 181.0)]
    public void Build_RejectsInvalidCoordinates(
        double latitude,
        double longitude)
    {
        var weather = CreateWeatherDataSet();

        var result = _builder.Build(
            new AnnualWeatherSolarProfileRequest(
                WeatherDataSet: weather,
                LatitudeDegrees: latitude,
                LongitudeDegrees: longitude));

        Assert.True(result.IsFailure);
    }

    private static AnnualWeatherDataSet CreateWeatherDataSet()
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

                return new HourlyWeatherRecord(
                    HourOfYear: hourOfYear,
                    Timestamp: timestamp,
                    Month: timestamp.Month,
                    Day: timestamp.Day,
                    Hour: timestamp.Hour,
                    DryBulbTemperatureC: 10,
                    DirectNormalIrradianceWm2: IsDayHour(timestamp.Hour) ? 600 : 0,
                    DiffuseHorizontalIrradianceWm2: IsDayHour(timestamp.Hour) ? 100 : 0,
                    GlobalHorizontalIrradianceWm2: null,
                    RelativeHumidityPercent: 50,
                    AtmosphericPressurePa: 101_325,
                    WindSpeedMPerS: 2.5,
                    WindDirectionDegrees: 180,
                    HorizontalInfraredRadiationWPerM2: 300,
                    SkyTemperatureC: 0,
                    TotalSkyCoverTenths: 5,
                    OpaqueSkyCoverTenths: 4);
            })
            .ToArray();

        return new AnnualWeatherDataSet(
            Year: 2026,
            TimeZoneOffset: TimeSpan.FromHours(5),
            Hours: hours);
    }

    private static bool IsDayHour(
        int hour) =>
        hour is >= 7 and <= 17;
}