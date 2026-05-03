using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public class SolarRadiationService : ISolarRadiationService
{
    private readonly ISolarPositionCalculator _solarPositionCalculator;
    private readonly ISurfaceIrradianceCalculator _surfaceIrradianceCalculator;

    public SolarRadiationService(
        ISolarPositionCalculator? solarPositionCalculator = null,
        ISurfaceIrradianceCalculator? surfaceIrradianceCalculator = null)
    {
        _solarPositionCalculator = solarPositionCalculator ?? new SolarPositionCalculator();
        _surfaceIrradianceCalculator = surfaceIrradianceCalculator ?? new PerezAnisotropicSurfaceIrradianceCalculator();
    }

    public double CalculateVerticalSurfaceRadiation(
        AnnualHourlyData hourlyData,
        CardinalDirection orientation,
        double latitude,
        int dayOfYear,
        int hour)
    {
        var timestamp = new DateTimeOffset(
                year: 2026,
                month: 1,
                day: 1,
                hour: Math.Clamp(hour, 0, 23),
                minute: 30,
                second: 0,
                offset: TimeSpan.Zero)
            .AddDays(Math.Clamp(dayOfYear, 1, 366) - 1);

        var solarPosition = _solarPositionCalculator.Calculate(
            new SolarPositionRequest(
                Timestamp: timestamp,
                LatitudeDegrees: latitude,
                LongitudeDegrees: 0));

        var globalHorizontalIrradiance = CalculateGlobalHorizontalIrradiance(
            hourlyData,
            solarPosition);

        var surface = WeatherSolarSurface.FromCardinalDirection(orientation);
        var result = _surfaceIrradianceCalculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: solarPosition,
                Surface: surface.Orientation,
                DirectNormalIrradianceWm2: hourlyData.DirectSolarRadiation,
                DiffuseHorizontalIrradianceWm2: hourlyData.DiffuseSolarRadiation,
                GlobalHorizontalIrradianceWm2: globalHorizontalIrradiance,
                GroundReflectance: 0.2,
                DiagnosticsContext: $"Annual climate hour {hourlyData.HourOfYear} {surface.Code} surface"));

        return Math.Max(0, result.TotalIrradianceWm2);
    }

    private static double CalculateGlobalHorizontalIrradiance(
        AnnualHourlyData hourlyData,
        SolarPositionResult solarPosition)
    {
        if (solarPosition.SolarAltitudeDegrees <= 0)
            return 0;

        var sunAltitudeRadians =
            solarPosition.SolarAltitudeDegrees *
            Math.PI /
            180.0;

        var projectedDirect =
            hourlyData.DirectSolarRadiation *
            Math.Sin(sunAltitudeRadians);

        return Math.Max(
            0.0,
            projectedDirect + hourlyData.DiffuseSolarRadiation);
    }
}
