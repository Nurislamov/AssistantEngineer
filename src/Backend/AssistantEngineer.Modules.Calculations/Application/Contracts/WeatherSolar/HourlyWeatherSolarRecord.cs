using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

public sealed record HourlyWeatherSolarRecord(
    int HourOfYear,
    HourlyWeatherRecord Weather,
    SolarPositionResult SolarPosition,
    IReadOnlyList<HourlySurfaceIrradianceRecord> SurfaceIrradiance)
{
    public HourlySurfaceIrradianceRecord GetSurface(
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
                $"Surface irradiance '{surfaceCode}' was not found for hour {HourOfYear}.");
        }

        return surface;
    }
}