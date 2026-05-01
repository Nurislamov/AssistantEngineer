using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Domain.Climate;

public sealed record WeatherRecord
{
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

    private WeatherRecord()
    {
    }

    private WeatherRecord(
        double dryBulbTemperature,
        double directSolarRadiation,
        double diffuseSolarRadiation,
        double? relativeHumidityPercent,
        double? atmosphericPressurePa,
        double? windSpeedMPerS,
        double? windDirectionDegrees,
        double? horizontalInfraredRadiationWPerM2,
        double? skyTemperatureC,
        double? totalSkyCoverTenths,
        double? opaqueSkyCoverTenths)
    {
        DryBulbTemperature = dryBulbTemperature;
        DirectSolarRadiation = directSolarRadiation;
        DiffuseSolarRadiation = diffuseSolarRadiation;
        RelativeHumidityPercent = relativeHumidityPercent;
        AtmosphericPressurePa = atmosphericPressurePa;
        WindSpeedMPerS = windSpeedMPerS;
        WindDirectionDegrees = windDirectionDegrees;
        HorizontalInfraredRadiationWPerM2 = horizontalInfraredRadiationWPerM2;
        SkyTemperatureC = skyTemperatureC;
        TotalSkyCoverTenths = totalSkyCoverTenths;
        OpaqueSkyCoverTenths = opaqueSkyCoverTenths;
    }

    public static Result<WeatherRecord> Create(
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
        var validation = Validate(
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

        if (validation.IsFailure)
            return Result<WeatherRecord>.Failure(validation);

        return Result<WeatherRecord>.Success(
            new WeatherRecord(
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
                opaqueSkyCoverTenths));
    }

    public static Result Validate(
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
        if (dryBulbTemperature is < -100 or > 100)
            return Result.Validation("Dry-bulb temperature out of reasonable range.");

        if (directSolarRadiation < 0 || diffuseSolarRadiation < 0)
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

        if (!IsValidOptional(totalSkyCoverTenths, 0, 10) ||
            !IsValidOptional(opaqueSkyCoverTenths, 0, 10))
        {
            return Result.Validation("Sky cover must be between 0 and 10 tenths.");
        }

        return Result.Success();
    }

    private static bool IsValidOptional(
        double? value,
        double min,
        double max) =>
        value is null ||
        (double.IsFinite(value.Value) &&
         value.Value >= min &&
         value.Value <= max);
}