using AssistantEngineer.Domain.Primitives;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Domain.Models.Climate;

public class AnnualClimateData
{
    public int Id { get; private set; }
    public int ClimateZoneId { get; private set; }
    public ClimateZone ClimateZone { get; private set; } = null!;
    public int Year { get; private set; } // Год, для которого данные (может быть типовым, например 2020)

    private readonly List<HourlyClimateData> _hourlyData = new();
    public IReadOnlyCollection<HourlyClimateData> HourlyData => new ReadOnlyCollection<HourlyClimateData>(_hourlyData);

    private AnnualClimateData() { }

    private AnnualClimateData(ClimateZone climateZone, int year)
    {
        ClimateZone = climateZone;
        ClimateZoneId = climateZone.Id;
        Year = year;
    }

    public static Result<AnnualClimateData> Create(ClimateZone climateZone, int year)
    {
        if (year < 1900 || year > 2100)
            return Result<AnnualClimateData>.Validation("Year must be between 1900 and 2100.");

        return Result<AnnualClimateData>.Success(new AnnualClimateData(climateZone, year));
    }

    public Result<HourlyClimateData> AddHourlyData(
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
            return Result<HourlyClimateData>.Validation("Hour of year must be between 0 and 8759.");

        if (_hourlyData.Any(h => h.HourOfYear == hourOfYear))
            return Result<HourlyClimateData>.Conflict($"Hourly data for hour {hourOfYear} already exists.");

        var hourly = HourlyClimateData.CreateAnnual(
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
