using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016WeatherSolarContextPerezDiagnosticsTests
{
    private readonly Iso52016WeatherSolarContextBuilder _builder =
        new(
            new AnnualClimateDataNormalizer(),
            new AnnualWeatherSolarProfileBuilder(
                new SolarPositionCalculator(),
                new PerezAnisotropicSurfaceIrradianceCalculator()),
            new PeriodicIso52016GroundBoundaryTemperatureProvider());

    [Fact]
    public void Build_PropagatesPerezAnisotropicDiagnostics()
    {
        var result = _builder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: CreateAnnualClimateData(),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess, result.Error);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.PerezAnisotropicModelUsed");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "Iso52016.PerezAnisotropicModelUsed");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.PerezSkyState");
    }

    private static AnnualClimateData CreateAnnualClimateData()
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
                directSolar: IsDayHour(hour) ? 600 : 0,
                diffuseSolar: IsDayHour(hour) ? 100 : 0,
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
