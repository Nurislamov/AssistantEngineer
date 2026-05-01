using AssistantEngineer.SharedKernel.Primitives;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.Climate;

public class AnnualClimateData
{
    public int Id { get; private set; }

    public int ClimateZoneId { get; private set; }

    public ClimateZone ClimateZone { get; private set; } = null!;

    public int Year { get; private set; }

    private readonly List<AnnualHourlyData> _hourlyData = new();

    public IReadOnlyCollection<AnnualHourlyData> HourlyData =>
        new ReadOnlyCollection<AnnualHourlyData>(_hourlyData);

    private AnnualClimateData()
    {
    }

    private AnnualClimateData(
        ClimateZone climateZone,
        int year)
    {
        ClimateZone = climateZone;
        ClimateZoneId = climateZone.Id;
        Year = year;
    }

    public static Result<AnnualClimateData> Create(
        ClimateZone climateZone,
        int year)
    {
        if (climateZone is null)
            return Result<AnnualClimateData>.Validation("Climate zone is required.");

        if (year < 1900 || year > 2100)
            return Result<AnnualClimateData>.Validation("Year must be between 1900 and 2100.");

        return Result<AnnualClimateData>.Success(
            new AnnualClimateData(
                climateZone,
                year));
    }

    public Result<AnnualHourlyData> AddHourlyData(
        int hourOfYear,
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
        if (hourOfYear < 0 || hourOfYear > 8759)
            return Result<AnnualHourlyData>.Validation("Hour of year must be between 0 and 8759.");

        if (_hourlyData.Any(h => h.HourOfYear == hourOfYear))
            return Result<AnnualHourlyData>.Conflict($"Hourly data for hour {hourOfYear} already exists.");

        var hourly = AnnualHourlyData.Create(
            this,
            hourOfYear,
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