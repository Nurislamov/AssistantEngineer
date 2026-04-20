using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Domain.Models.Climate;

public class HourlyClimateData
{
    public int Id { get; private set; }
    public int? ClimateDataId { get; private set; }
    public ClimateData? ClimateData { get; private set; }
    public int? AnnualClimateDataId { get; private set; }
    public AnnualClimateData? AnnualClimateData { get; private set; }

    public int? Hour { get; private set; }
    public int? HourOfYear { get; private set; }

    public double DryBulbTemperature { get; private set; }
    public double DirectSolarRadiation { get; private set; }
    public double DiffuseSolarRadiation { get; private set; }
    public double? RelativeHumidityPercent { get; private set; }
    public double? AtmosphericPressurePa { get; private set; }
    public double? WindSpeedMPerS { get; private set; }
    public double? WindDirectionDegrees { get; private set; }
    public double? HorizontalInfraredRadiationWPerM2 { get; private set; }
    public double? SkyTemperatureC { get; private set; }
    public double? TotalSkyCoverTenths { get; private set; }
    public double? OpaqueSkyCoverTenths { get; private set; }

    private HourlyClimateData() { }

    private HourlyClimateData(
        ClimateData climateData,
        int hour,
        double dryBulbTemp,
        double directSolar,
        double diffuseSolar,
        double? relativeHumidityPercent,
        double? atmosphericPressurePa,
        double? windSpeedMPerS,
        double? windDirectionDegrees,
        double? horizontalInfraredRadiationWPerM2,
        double? skyTemperatureC,
        double? totalSkyCoverTenths,
        double? opaqueSkyCoverTenths)
    {
        ClimateData = climateData;
        ClimateDataId = climateData.Id;
        Hour = hour;
        DryBulbTemperature = dryBulbTemp;
        DirectSolarRadiation = directSolar;
        DiffuseSolarRadiation = diffuseSolar;
        RelativeHumidityPercent = relativeHumidityPercent;
        AtmosphericPressurePa = atmosphericPressurePa;
        WindSpeedMPerS = windSpeedMPerS;
        WindDirectionDegrees = windDirectionDegrees;
        HorizontalInfraredRadiationWPerM2 = horizontalInfraredRadiationWPerM2;
        SkyTemperatureC = skyTemperatureC;
        TotalSkyCoverTenths = totalSkyCoverTenths;
        OpaqueSkyCoverTenths = opaqueSkyCoverTenths;
    }

    private HourlyClimateData(
        AnnualClimateData annualClimateData,
        int hourOfYear,
        double dryBulbTemp,
        double directSolar,
        double diffuseSolar,
        double? relativeHumidityPercent,
        double? atmosphericPressurePa,
        double? windSpeedMPerS,
        double? windDirectionDegrees,
        double? horizontalInfraredRadiationWPerM2,
        double? skyTemperatureC,
        double? totalSkyCoverTenths,
        double? opaqueSkyCoverTenths)
    {
        AnnualClimateData = annualClimateData;
        AnnualClimateDataId = annualClimateData.Id;
        HourOfYear = hourOfYear;
        DryBulbTemperature = dryBulbTemp;
        DirectSolarRadiation = directSolar;
        DiffuseSolarRadiation = diffuseSolar;
        RelativeHumidityPercent = relativeHumidityPercent;
        AtmosphericPressurePa = atmosphericPressurePa;
        WindSpeedMPerS = windSpeedMPerS;
        WindDirectionDegrees = windDirectionDegrees;
        HorizontalInfraredRadiationWPerM2 = horizontalInfraredRadiationWPerM2;
        SkyTemperatureC = skyTemperatureC;
        TotalSkyCoverTenths = totalSkyCoverTenths;
        OpaqueSkyCoverTenths = opaqueSkyCoverTenths;
    }

    public static Result<HourlyClimateData> Create(
        ClimateData climateData,
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
        var validation = ValidateWeatherValues(
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
        if (validation.IsFailure)
            return Result<HourlyClimateData>.Failure(validation);

        return Result<HourlyClimateData>.Success(new HourlyClimateData(
            climateData,
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
            opaqueSkyCoverTenths));
    }

    public static Result<HourlyClimateData> CreateAnnual(
        AnnualClimateData annualClimateData,
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
        var validation = ValidateWeatherValues(
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
        if (validation.IsFailure)
            return Result<HourlyClimateData>.Failure(validation);

        return Result<HourlyClimateData>.Success(new HourlyClimateData(
            annualClimateData,
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
            opaqueSkyCoverTenths));
    }

    private static Result ValidateWeatherValues(
        double dryBulbTemp,
        double directSolar,
        double diffuseSolar,
        double? relativeHumidityPercent,
        double? atmosphericPressurePa,
        double? windSpeedMPerS,
        double? windDirectionDegrees,
        double? horizontalInfraredRadiationWPerM2,
        double? skyTemperatureC,
        double? totalSkyCoverTenths,
        double? opaqueSkyCoverTenths)
    {
        if (dryBulbTemp < -100 || dryBulbTemp > 100)
            return Result.Validation("Dry-bulb temperature out of reasonable range.");
        if (directSolar < 0 || diffuseSolar < 0)
            return Result.Validation("Solar radiation cannot be negative.");

        if (!IsValidOptional(relativeHumidityPercent, 0, 100))
            return Result.Validation("Relative humidity must be between 0 and 100 percent.");

        if (!IsValidOptional(atmosphericPressurePa, 30_000, 120_000))
            return Result.Validation("Atmospheric pressure out of reasonable range.");

        if (!IsValidOptional(windSpeedMPerS, 0, 100))
            return Result.Validation("Wind speed out of reasonable range.");

        if (!IsValidOptional(windDirectionDegrees, 0, 360))
            return Result.Validation("Wind direction must be between 0 and 360 degrees.");

        if (!IsValidOptional(horizontalInfraredRadiationWPerM2, 0, 1000))
            return Result.Validation("Horizontal infrared radiation out of reasonable range.");

        if (!IsValidOptional(skyTemperatureC, -150, 80))
            return Result.Validation("Sky temperature out of reasonable range.");

        if (!IsValidOptional(totalSkyCoverTenths, 0, 10) || !IsValidOptional(opaqueSkyCoverTenths, 0, 10))
            return Result.Validation("Sky cover must be between 0 and 10 tenths.");

        return Result.Success();
    }

    private static bool IsValidOptional(double? value, double min, double max) =>
        value is null || (double.IsFinite(value.Value) && value.Value >= min && value.Value <= max);
}
