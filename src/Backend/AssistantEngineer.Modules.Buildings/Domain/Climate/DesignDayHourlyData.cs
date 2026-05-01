using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Domain.Climate;

public class DesignDayHourlyData
{
    public int Id { get; private set; }

    public int ClimateDataId { get; private set; }

    public ClimateData ClimateData { get; private set; } = null!;

    public int Hour { get; private set; }

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

    private DesignDayHourlyData()
    {
    }

    private DesignDayHourlyData(
        ClimateData climateData,
        int hour,
        WeatherRecord weather)
    {
        ClimateData = climateData;
        ClimateDataId = climateData.Id;
        Hour = hour;
        Weather = weather;
    }

    public static Result<DesignDayHourlyData> Create(
        ClimateData climateData,
        int hour,
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
        if (climateData is null)
            return Result<DesignDayHourlyData>.Validation("Climate data is required.");

        if (hour is < 0 or > 23)
            return Result<DesignDayHourlyData>.Validation("Hour must be between 0 and 23.");

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
            return Result<DesignDayHourlyData>.Failure(weather);

        return Result<DesignDayHourlyData>.Success(
            new DesignDayHourlyData(
                climateData,
                hour,
                weather.Value));
    }
}