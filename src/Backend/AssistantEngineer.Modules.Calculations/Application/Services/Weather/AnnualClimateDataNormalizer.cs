using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Weather;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Weather;

public sealed class AnnualClimateDataNormalizer : IAnnualWeatherDataNormalizer
{
    public Result<AnnualWeatherDataSet> Normalize(
        AnnualWeatherNormalizationRequest request)
    {
        if (request.AnnualClimateData is null)
        {
            return Result<AnnualWeatherDataSet>.Validation(
                "Annual climate data is required.");
        }

        var validation = ValidateAnnualClimateData(
            request.AnnualClimateData);

        if (validation.IsFailure)
            return Result<AnnualWeatherDataSet>.Failure(validation);

        var orderedHours = request.AnnualClimateData.HourlyData
            .OrderBy(hour => hour.HourOfYear!.Value)
            .Select(hour => MapHour(
                request.AnnualClimateData.Year,
                request.TimeZoneOffset,
                hour))
            .ToArray();

        var dataSet = new AnnualWeatherDataSet(
            Year: request.AnnualClimateData.Year,
            TimeZoneOffset: request.TimeZoneOffset,
            Hours: orderedHours);

        return Result<AnnualWeatherDataSet>.Success(dataSet);
    }

    private static Result ValidateAnnualClimateData(
        AnnualClimateData annualClimateData)
    {
        if (annualClimateData.Year is < 1900 or > 2100)
        {
            return Result.Validation(
                "Annual weather year must be between 1900 and 2100.");
        }

        if (annualClimateData.HourlyData.Count == 0)
        {
            return Result.Validation(
                "Annual climate data must contain hourly records.");
        }

        var hoursWithoutHourOfYear = annualClimateData.HourlyData
            .Where(hour => !hour.HourOfYear.HasValue)
            .ToArray();

        if (hoursWithoutHourOfYear.Length > 0)
        {
            return Result.Validation(
                "Every annual climate hourly record must have HourOfYear.");
        }

        var duplicateHours = annualClimateData.HourlyData
            .GroupBy(hour => hour.HourOfYear!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order()
            .ToArray();

        if (duplicateHours.Length > 0)
        {
            return Result.Conflict(
                $"Annual climate data contains duplicate hours: {string.Join(", ", duplicateHours)}.");
        }

        var expectedHourCount = DateTime.IsLeapYear(annualClimateData.Year)
            ? AnnualWeatherDataSet.LeapYearHourCount
            : AnnualWeatherDataSet.NonLeapYearHourCount;

        // Текущая доменная модель AnnualClimateData поддерживает 0..8759.
        // Поэтому для високосного года пока допускаем только non-leap normalized dataset.
        if (annualClimateData.HourlyData.Count != AnnualWeatherDataSet.NonLeapYearHourCount)
        {
            return Result.Validation(
                $"Annual climate data must contain {AnnualWeatherDataSet.NonLeapYearHourCount} hourly records.");
        }

        var expectedHours = Enumerable
            .Range(0, AnnualWeatherDataSet.NonLeapYearHourCount)
            .ToHashSet();

        var actualHours = annualClimateData.HourlyData
            .Select(hour => hour.HourOfYear!.Value)
            .ToHashSet();

        expectedHours.ExceptWith(actualHours);

        if (expectedHours.Count > 0)
        {
            return Result.Validation(
                $"Annual climate data is missing hourly records: {string.Join(", ", expectedHours.Order().Take(20))}.");
        }

        return Result.Success();
    }

    private static HourlyWeatherRecord MapHour(
        int year,
        TimeSpan timeZoneOffset,
        HourlyClimateData source)
    {
        var hourOfYear = source.HourOfYear!.Value;

        var timestamp = new DateTimeOffset(
                year,
                month: 1,
                day: 1,
                hour: 0,
                minute: 0,
                second: 0,
                offset: timeZoneOffset)
            .AddHours(hourOfYear);

        return new HourlyWeatherRecord(
            HourOfYear: hourOfYear,
            Timestamp: timestamp,
            Month: timestamp.Month,
            Day: timestamp.Day,
            Hour: timestamp.Hour,
            DryBulbTemperatureC: source.DryBulbTemperature,
            DirectNormalIrradianceWm2: source.DirectSolarRadiation,
            DiffuseHorizontalIrradianceWm2: source.DiffuseSolarRadiation,
            GlobalHorizontalIrradianceWm2: null,
            RelativeHumidityPercent: source.RelativeHumidityPercent,
            AtmosphericPressurePa: source.AtmosphericPressurePa,
            WindSpeedMPerS: source.WindSpeedMPerS,
            WindDirectionDegrees: source.WindDirectionDegrees,
            HorizontalInfraredRadiationWPerM2: source.HorizontalInfraredRadiationWPerM2,
            SkyTemperatureC: source.SkyTemperatureC,
            TotalSkyCoverTenths: source.TotalSkyCoverTenths,
            OpaqueSkyCoverTenths: source.OpaqueSkyCoverTenths);
    }
}