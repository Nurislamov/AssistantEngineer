namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;

public sealed record HourlyWeatherRecord(
    int HourOfYear,
    DateTimeOffset Timestamp,
    int Month,
    int Day,
    int Hour,
    double DryBulbTemperatureC,
    double DirectNormalIrradianceWm2,
    double DiffuseHorizontalIrradianceWm2,
    double? GlobalHorizontalIrradianceWm2 = null,
    double? RelativeHumidityPercent = null,
    double? AtmosphericPressurePa = null,
    double? WindSpeedMPerS = null,
    double? WindDirectionDegrees = null,
    double? HorizontalInfraredRadiationWPerM2 = null,
    double? SkyTemperatureC = null,
    double? TotalSkyCoverTenths = null,
    double? OpaqueSkyCoverTenths = null);