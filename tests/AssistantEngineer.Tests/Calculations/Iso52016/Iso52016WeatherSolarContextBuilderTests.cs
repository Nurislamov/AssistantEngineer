using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016WeatherSolarContextBuilderTests
{
    private readonly Iso52016WeatherSolarContextBuilder _builder =
        new(
            new AnnualClimateDataNormalizer(),
            new AnnualWeatherSolarProfileBuilder(
                new SolarPositionCalculator(),
                new PerezAnisotropicSurfaceIrradianceCalculator()),
            new PeriodicIso52016GroundBoundaryTemperatureProvider());

    [Fact]
    public void Build_CreatesCompleteIso52016WeatherSolarContext()
    {
        var annualClimateData = CreateAnnualClimateData();

        var result = _builder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess);

        var context = result.Value;

        Assert.Equal(2026, context.Year);
        Assert.Equal(TimeSpan.FromHours(5), context.TimeZoneOffset);
        Assert.Equal(41.3, context.LatitudeDegrees);
        Assert.Equal(69.2, context.LongitudeDegrees);
        Assert.Equal(AnnualWeatherDataSet.NonLeapYearHourCount, context.HourCount);

        var firstHour = context.GetHour(0);

        Assert.Equal(0, firstHour.HourOfYear);
        Assert.Equal(1, firstHour.Month);
        Assert.Equal(1, firstHour.Day);
        Assert.Equal(0, firstHour.Hour);
        Assert.Equal(10, firstHour.OutdoorTemperatureC);

        Assert.NotEmpty(firstHour.SurfaceIrradiance);
        Assert.Contains(context.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.HourlyWeatherSourceUsed");
        Assert.Contains(context.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.AnnualClimateSolarDataUsed");
        Assert.Contains(context.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.SurfaceIrradianceCalculated");
    }

    [Fact]
    public void Build_MapsDefaultSurfaces()
    {
        var annualClimateData = CreateAnnualClimateData();

        var result = _builder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess);

        var noon = result.Value.GetHour(12);

        Assert.NotNull(noon.GetSurface("horizontal"));
        Assert.NotNull(noon.GetSurface("north"));
        Assert.NotNull(noon.GetSurface("east"));
        Assert.NotNull(noon.GetSurface("south"));
        Assert.NotNull(noon.GetSurface("west"));
    }

    [Fact]
    public void Build_ProvidesSolarPositionForEachHour()
    {
        var annualClimateData = CreateAnnualClimateData();

        var result = _builder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess);

        var noon = result.Value.GetHour(12);

        Assert.InRange(
            noon.SolarAzimuthDegrees,
            0,
            360);

        Assert.InRange(
            noon.SolarAltitudeDegrees,
            -90,
            90);
    }

    [Fact]
    public void Build_ClampsNightSurfaceIrradianceToZero()
    {
        var annualClimateData = CreateAnnualClimateData(
            forceNightSolarRadiation: true);

        var result = _builder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess);

        var midnight = result.Value.GetHour(0);
        var south = midnight.GetSurface("south");

        Assert.Equal(0, south.BeamIrradianceWm2, precision: 6);
        Assert.Equal(0, south.DiffuseSkyIrradianceWm2, precision: 6);
        Assert.Equal(0, south.GroundReflectedIrradianceWm2, precision: 6);
        Assert.Equal(0, south.TotalIrradianceWm2, precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.NightSolarClampedToZero");
    }

    [Fact]
    public void Build_RejectsInvalidLatitude()
    {
        var annualClimateData = CreateAnnualClimateData();

        var result = _builder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: 91,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Equal("Latitude must be between -90 and 90 degrees.", result.Error);
    }

    private static AnnualClimateData CreateAnnualClimateData(
        bool forceNightSolarRadiation = false)
    {
        var climateZone = CreateClimateZone();

        var annualDataResult = AnnualClimateData.Create(
            climateZone,
            year: 2026);

        Assert.True(annualDataResult.IsSuccess);

        var annualData = annualDataResult.Value;

        for (var hour = 0; hour < AnnualWeatherDataSet.NonLeapYearHourCount; hour++)
        {
            var addResult = annualData.AddHourlyData(
                hourOfYear: hour,
                dryBulbTemp: 10,
                directSolar: IsDayHour(hour) || forceNightSolarRadiation ? 600 : 0,
                diffuseSolar: IsDayHour(hour) || forceNightSolarRadiation ? 100 : 0,
                relativeHumidityPercent: 50,
                atmosphericPressurePa: 101_325,
                windSpeedMPerS: 2.5,
                windDirectionDegrees: 180,
                horizontalInfraredRadiationWPerM2: 300,
                skyTemperatureC: 0,
                totalSkyCoverTenths: 5,
                opaqueSkyCoverTenths: 4);

            Assert.True(addResult.IsSuccess);
        }

        return annualData;
    }

    private static bool IsDayHour(
        int hourOfYear)
    {
        var hour = hourOfYear % 24;

        return hour is >= 7 and <= 17;
    }

    private static ClimateZone CreateClimateZone()
    {
        var summer = Temperature.FromCelsius(35);
        var winter = Temperature.FromCelsius(-5);

        Assert.True(summer.IsSuccess);
        Assert.True(winter.IsSuccess);

        var climateZone = ClimateZone.Create(
            "Test climate zone",
            summer.Value,
            winter.Value);

        Assert.True(climateZone.IsSuccess);

        return climateZone.Value;
    }
}

