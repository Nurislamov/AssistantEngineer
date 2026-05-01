using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Domain.Climate;

public class AnnualHourlyData
{
    public int Id { get; private set; }

    public int AnnualClimateDataId { get; private set; }

    public AnnualClimateData AnnualClimateData { get; private set; } = null!;

    public int HourOfYear { get; private set; }

    public WeatherRecord Weather { get; private set; } = null!;

    public double DryBulbTemperature => Weather.DryBulbTemperature;

    public double DirectSolarRadiation => Weather.DirectSolarRadiation;

    public double DiffuseSolarRadiation => Weather.DiffuseSolarRadiation;

    public double? RelativeHumidityPercent => Weather.RelativeHumidityPercent;

    public double? AtmosphericPressurePa => Weather.AtmosphericPressurePa;

    public double? WindSpeedMPerS => Weather.WindSpeedMPerS;

    public double? WindDirectionDegrees => Weather.WindDirectionDegrees;

    public double? HorizontalInfraredRadiationWPerM2 => Weather.HorizontalInfraredRadiationWPerM2;

    public double? SkyTemperatureC => Weather.SkyTemperatureC;

    public double? TotalSkyCoverTenths => Weather.TotalSkyCoverTenths;

    public double? OpaqueSkyCoverTenths => Weather.OpaqueSkyCoverTenths;

    private AnnualHourlyData()
    {
    }

    private AnnualHourlyData(
        AnnualClimateData annualClimateData,
        int hourOfYear,
        WeatherRecord weather)
    {
        AnnualClimateData = annualClimateData;
        AnnualClimateDataId = annualClimateData.Id;
        HourOfYear = hourOfYear;
        Weather = weather;
    }

    public static Result<AnnualHourlyData> Create(
        AnnualClimateData annualClimateData,
        int hourOfYear,
        double dryBulbTemperature,
        double directSolarRadiation,
        double diffuseSolarRadiation,
        double? relativeHumidityPercent = null,
        double? atmosphericPressurePa = null,
        double? windSpeedMPerS = null,
        double? windDirectionDegrees = null,
        double? horizontalInfraredRadiationWPerM2 = null,
        double? skyTemperatureC = null,
        double? totalSkyCoverTenths = null,
        double? opaqueSkyCoverTenths = null)
    {
        if (annualClimateData is null)
            return Result<AnnualHourlyData>.Validation("Annual climate data is required.");

        if (hourOfYear is < 0 or > 8759)
            return Result<AnnualHourlyData>.Validation("Hour of year must be between 0 and 8759.");

        var weather = WeatherRecord.Create(
            dryBulbTemperature,
            directSolarRadiation,
            diffuseSolarRadiation,
            relativeHumidityPercent,
            atmosphericPressurePa,
            windSpeedMPerS,
            windDirectionDegrees,
            horizontalInfraredRadiationWPerM2,
            skyTemperatureC,
            totalSkyCoverTenths,
            opaqueSkyCoverTenths);

        if (weather.IsFailure)
            return Result<AnnualHourlyData>.Failure(weather);

        return Result<AnnualHourlyData>.Success(
            new AnnualHourlyData(
                annualClimateData,
                hourOfYear,
                weather.Value));
    }
}