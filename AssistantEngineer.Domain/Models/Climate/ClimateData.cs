using AssistantEngineer.Domain.Primitives;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Domain.Models.Climate;

public class ClimateData
{
    public int Id { get; private set; }
    public int ClimateZoneId { get; private set; }
    public ClimateZone ClimateZone { get; private set; } = null!;
    public int Month { get; private set; }
    public int DayOfMonth { get; private set; }
    public double DailyTemperatureRange { get; private set; }

    private readonly List<HourlyClimateData> _hourlyData = new();
    public IReadOnlyCollection<HourlyClimateData> HourlyData => new ReadOnlyCollection<HourlyClimateData>(_hourlyData);

    private ClimateData() { }

    private ClimateData(ClimateZone climateZone, int month, int dayOfMonth, double dailyTemperatureRange)
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
        if (month < 1 || month > 12)
            return Result<ClimateData>.Validation("Month must be between 1 and 12.");
        if (dayOfMonth < 1 || dayOfMonth > 31)
            return Result<ClimateData>.Validation("Day of month must be between 1 and 31.");
        if (dailyTemperatureRange < 0)
            return Result<ClimateData>.Validation("Daily temperature range cannot be negative.");

        return Result<ClimateData>.Success(new ClimateData(climateZone, month, dayOfMonth, dailyTemperatureRange));
    }

    public Result<HourlyClimateData> AddHourlyData(
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
            return Result<HourlyClimateData>.Validation("Hour must be between 0 and 23.");

        if (_hourlyData.Any(h => h.Hour == hour))
            return Result<HourlyClimateData>.Conflict($"Hourly data for hour {hour} already exists.");

        var hourly = HourlyClimateData.Create(
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
