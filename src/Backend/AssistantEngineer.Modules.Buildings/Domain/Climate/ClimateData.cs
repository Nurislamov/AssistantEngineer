using AssistantEngineer.SharedKernel.Primitives;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.Climate;

public class ClimateData
{
    public int Id { get; private set; }

    public int ClimateZoneId { get; private set; }

    public ClimateZone ClimateZone { get; private set; } = null!;

    public int Month { get; private set; }

    public int DayOfMonth { get; private set; }

    public double DailyTemperatureRange { get; private set; }

    private readonly List<DesignDayHourlyData> _hourlyData = new();

    public IReadOnlyCollection<DesignDayHourlyData> HourlyData =>
        new ReadOnlyCollection<DesignDayHourlyData>(_hourlyData);

    private ClimateData()
    {
    }

    private ClimateData(
        ClimateZone climateZone,
        int month,
        int dayOfMonth,
        double dailyTemperatureRange)
    {
        ClimateZone = climateZone;
        ClimateZoneId = climateZone.Id;
        Month = month;
        DayOfMonth = dayOfMonth;
        DailyTemperatureRange = dailyTemperatureRange;
    }

    public static Result<ClimateData> Create(
        ClimateZone climateZone,
        int month,
        int dayOfMonth,
        double dailyTemperatureRange)
    {
        if (climateZone is null)
            return Result<ClimateData>.Validation("Climate zone is required.");

        if (month < 1 || month > 12)
            return Result<ClimateData>.Validation("Month must be between 1 and 12.");

        if (dayOfMonth < 1 || dayOfMonth > 31)
            return Result<ClimateData>.Validation("Day of month must be between 1 and 31.");

        if (dailyTemperatureRange < 0)
            return Result<ClimateData>.Validation("Daily temperature range cannot be negative.");

        return Result<ClimateData>.Success(
            new ClimateData(
                climateZone,
                month,
                dayOfMonth,
                dailyTemperatureRange));
    }

    public Result<DesignDayHourlyData> AddHourlyData(
        int hour,
        double dryBulbTemp,
        double directSolar,
        double diffuseSolar,
        double? relativeHumidityPercent = null,
        double? atmosphericPressurePa = null,
        double? windSpeedMPerS = null,
        double? windDirectionDegrees = null,
        double? horizontalInfraredRadiationWPerM2 = null,
        double? skyTemperatureC = null,
        double? totalSkyCoverTenths = null,
        double? opaqueSkyCoverTenths = null)
    {
        if (hour < 0 || hour > 23)
            return Result<DesignDayHourlyData>.Validation("Hour must be between 0 and 23.");

        if (_hourlyData.Any(h => h.Hour == hour))
            return Result<DesignDayHourlyData>.Conflict($"Hourly data for hour {hour} already exists.");

        var hourly = DesignDayHourlyData.Create(
            this,
            hour,
            dryBulbTemp,
            directSolar,
            diffuseSolar,
            relativeHumidityPercent,
            atmosphericPressurePa,
            windSpeedMPerS,
            windDirectionDegrees,
            horizontalInfraredRadiationWPerM2,
            skyTemperatureC,
            totalSkyCoverTenths,
            opaqueSkyCoverTenths);

        if (hourly.IsFailure)
            return hourly;

        _hourlyData.Add(hourly.Value);
        return hourly;
    }
}