using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016WeatherSolarWindowGainIntegrationTests
{
    private readonly Iso52016WeatherSolarContextBuilder _contextBuilder =
        new(
            new AnnualClimateDataNormalizer(),
            new AnnualWeatherSolarProfileBuilder(
                new SolarPositionCalculator(),
                new PerezAnisotropicSurfaceIrradianceCalculator()),
            new PeriodicIso52016GroundBoundaryTemperatureProvider());

    [Fact]
    public void Build_ProvidesPerezComponentIrradianceToWindowSolarGainCalculator()
    {
        var contextResult = _contextBuilder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: CreateAnnualClimateData(),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(contextResult.IsSuccess, contextResult.Error);

        var context = contextResult.Value;

        Assert.Contains(context.Diagnostics, diagnostic =>
            diagnostic.Code == "Iso52016.PerezAnisotropicModelUsed");

        var daylightHour = context.Hours
            .Where(hour => hour.GetSurface("south").TotalIrradianceWm2 > 0)
            .OrderByDescending(hour => hour.GetSurface("south").TotalIrradianceWm2)
            .First();

        var southSurface = daylightHour.GetSurface("south");

        Assert.True(southSurface.BeamIrradianceWm2 > 0);
        Assert.True(southSurface.DiffuseSkyIrradianceWm2 > 0);
        Assert.True(southSurface.TotalIrradianceWm2 > 0);

        var gainResult = new Iso52016WindowSolarGainCalculator()
            .Calculate(
                new Iso52016WindowSolarGainRequest(
                    Hour: daylightHour,
                    Orientation: CardinalDirection.South,
                    WindowAreaM2: 4.0,
                    SolarHeatGainCoefficient: 0.55,
                    FrameFraction: 0.15,
                    ShadingFactor: 0.8));

        Assert.True(gainResult.IsSuccess, gainResult.Error);

        var gain = gainResult.Value;

        Assert.Equal(
            southSurface.TotalIrradianceWm2,
            gain.SurfaceTotalIrradianceWm2,
            precision: 6);

        Assert.True(gain.BeamSolarGainW > 0);
        Assert.True(gain.DiffuseSkySolarGainW > 0);
        Assert.True(gain.TotalSolarGainW > 0);

        Assert.Contains(gain.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarGains.ComponentIrradianceProvided");
    }

    [Fact]
    public void Build_KeepsNightZeroClampBeforeWindowSolarGainCalculation()
    {
        var contextResult = _contextBuilder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: CreateAnnualClimateData(forceNightSolarRadiation: true),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(contextResult.IsSuccess, contextResult.Error);

        var midnight = contextResult.Value.GetHour(0);
        var southSurface = midnight.GetSurface("south");

        Assert.Equal(0.0, southSurface.BeamIrradianceWm2, precision: 6);
        Assert.Equal(0.0, southSurface.DiffuseSkyIrradianceWm2, precision: 6);
        Assert.Equal(0.0, southSurface.GroundReflectedIrradianceWm2, precision: 6);
        Assert.Equal(0.0, southSurface.TotalIrradianceWm2, precision: 6);

        var gainResult = new Iso52016WindowSolarGainCalculator()
            .Calculate(
                new Iso52016WindowSolarGainRequest(
                    Hour: midnight,
                    Orientation: CardinalDirection.South,
                    WindowAreaM2: 4.0,
                    SolarHeatGainCoefficient: 0.55));

        Assert.True(gainResult.IsSuccess, gainResult.Error);
        Assert.Equal(0.0, gainResult.Value.TotalSolarGainW, precision: 6);
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
