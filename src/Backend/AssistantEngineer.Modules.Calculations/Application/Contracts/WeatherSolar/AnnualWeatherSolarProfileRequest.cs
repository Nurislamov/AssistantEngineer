using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

public sealed record AnnualWeatherSolarProfileRequest(
    AnnualWeatherDataSet WeatherDataSet,
    double LatitudeDegrees,
    double LongitudeDegrees,
    IReadOnlyList<WeatherSolarSurface>? Surfaces = null,
    double GroundReflectance = 0.2);