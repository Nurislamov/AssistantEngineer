using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyWeatherSolarRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double OutdoorTemperatureC,
    double GroundBoundaryTemperatureC,
    double SolarAltitudeDegrees,
    double SolarAzimuthDegrees,
    double DirectNormalIrradianceWm2,
    double DiffuseHorizontalIrradianceWm2,
    double GlobalHorizontalIrradianceWm2,
    IReadOnlyList<Iso52016SurfaceWeatherSolarRecord> SurfaceIrradiance)
{
    public Iso52016SurfaceWeatherSolarRecord GetSurface(
        CardinalDirection orientation) =>
        GetSurface(
            WeatherSolarSurfaceCodes.FromCardinalDirection(orientation));

    public Iso52016SurfaceWeatherSolarRecord GetSurface(
        string surfaceCode)
    {
        var surface = SurfaceIrradiance.FirstOrDefault(record =>
            string.Equals(
                record.SurfaceCode,
                surfaceCode,
                StringComparison.OrdinalIgnoreCase));

        if (surface is null)
        {
            throw new KeyNotFoundException(
                $"Surface '{surfaceCode}' was not found for ISO 52016 hour {HourOfYear}.");
        }

        return surface;
    }
}