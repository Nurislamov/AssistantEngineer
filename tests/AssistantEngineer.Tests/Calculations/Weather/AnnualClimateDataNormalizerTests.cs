using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Weather;

public class AnnualClimateDataNormalizerTests
{
    private readonly AnnualClimateDataNormalizer _normalizer = new();

    [Fact]
    public void Normalize_ReturnsCompleteNonLeapYearDataSet()
    {
        var annualData = CreateAnnualClimateData(
            year: 2026,
            hourCount: AnnualWeatherDataSet.NonLeapYearHourCount);

        var result = _normalizer.Normalize(
            new AnnualWeatherNormalizationRequest(
                annualData,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess);

        var dataSet = result.Value;

        Assert.Equal(2026, dataSet.Year);
        Assert.Equal(TimeSpan.FromHours(5), dataSet.TimeZoneOffset);
        Assert.Equal(AnnualWeatherDataSet.NonLeapYearHourCount, dataSet.HourCount);
        Assert.True(dataSet.IsCompleteNonLeapYear);
        Assert.True(dataSet.IsCompleteYear());
    }

    [Fact]
    public void Normalize_CreatesExpectedTimestamps()
    {
        var annualData = CreateAnnualClimateData(
            year: 2026,
            hourCount: AnnualWeatherDataSet.NonLeapYearHourCount);

        var result = _normalizer.Normalize(
            new AnnualWeatherNormalizationRequest(
                annualData,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess);

        var firstHour = result.Value.GetHour(0);
        var secondHour = result.Value.GetHour(1);
        var lastHour = result.Value.GetHour(8759);

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(5)), firstHour.Timestamp);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.FromHours(5)), secondHour.Timestamp);
        Assert.Equal(new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.FromHours(5)), lastHour.Timestamp);

        Assert.Equal(1, firstHour.Month);
        Assert.Equal(1, firstHour.Day);
        Assert.Equal(0, firstHour.Hour);

        Assert.Equal(12, lastHour.Month);
        Assert.Equal(31, lastHour.Day);
        Assert.Equal(23, lastHour.Hour);
    }

    [Fact]
    public void Normalize_PreservesWeatherValues()
    {
        var annualData = CreateAnnualClimateData(
            year: 2026,
            hourCount: AnnualWeatherDataSet.NonLeapYearHourCount);

        var result = _normalizer.Normalize(
            new AnnualWeatherNormalizationRequest(
                annualData,
                TimeZoneOffset: TimeSpan.Zero));

        Assert.True(result.IsSuccess);

        var hour = result.Value.GetHour(123);

        Assert.Equal(123, hour.HourOfYear);
        Assert.Equal(10 + 123 % 20, hour.DryBulbTemperatureC);
        Assert.Equal(500 + 123 % 100, hour.DirectNormalIrradianceWm2);
        Assert.Equal(100 + 123 % 50, hour.DiffuseHorizontalIrradianceWm2);
        Assert.Equal(50, hour.RelativeHumidityPercent);
        Assert.Equal(101_325, hour.AtmosphericPressurePa);
        Assert.Equal(2.5, hour.WindSpeedMPerS);
        Assert.Equal(180, hour.WindDirectionDegrees);
        Assert.Equal(300, hour.HorizontalInfraredRadiationWPerM2);
        Assert.Equal(-5, hour.SkyTemperatureC);
        Assert.Equal(5, hour.TotalSkyCoverTenths);
        Assert.Equal(4, hour.OpaqueSkyCoverTenths);
    }

    [Fact]
    public void Normalize_RejectsMissingHours()
    {
        var annualData = CreateAnnualClimateData(
            year: 2026,
            hourCount: AnnualWeatherDataSet.NonLeapYearHourCount - 1);

        var result = _normalizer.Normalize(
            new AnnualWeatherNormalizationRequest(
                annualData,
                TimeZoneOffset: TimeSpan.Zero));

        Assert.True(result.IsFailure);
        Assert.Equal("Annual climate data must contain 8760 hourly records.", result.Error);
    }

    [Fact]
    public void GetHour_RejectsOutOfRangeHour()
    {
        var annualData = CreateAnnualClimateData(
            year: 2026,
            hourCount: AnnualWeatherDataSet.NonLeapYearHourCount);

        var result = _normalizer.Normalize(
            new AnnualWeatherNormalizationRequest(
                annualData,
                TimeZoneOffset: TimeSpan.Zero));

        Assert.True(result.IsSuccess);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.Value.GetHour(-1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.Value.GetHour(8760));
    }

    private static AnnualClimateData CreateAnnualClimateData(
        int year,
        int hourCount)
    {
        var climateZone = CreateClimateZone();

        var annualDataResult = AnnualClimateData.Create(
            climateZone,
            year);

        Assert.True(annualDataResult.IsSuccess);

        var annualData = annualDataResult.Value;

        for (var hour = 0; hour < hourCount; hour++)
        {
            var addResult = annualData.AddHourlyData(
                hourOfYear: hour,
                dryBulbTemp: 10 + hour % 20,
                directSolar: 500 + hour % 100,
                diffuseSolar: 100 + hour % 50,
                relativeHumidityPercent: 50,
                atmosphericPressurePa: 101_325,
                windSpeedMPerS: 2.5,
                windDirectionDegrees: 180,
                horizontalInfraredRadiationWPerM2: 300,
                skyTemperatureC: -5,
                totalSkyCoverTenths: 5,
                opaqueSkyCoverTenths: 4);

            Assert.True(addResult.IsSuccess);
        }

        return annualData;
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