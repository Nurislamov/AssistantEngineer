using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016WeatherSolarContextRequest(
    AnnualClimateData AnnualClimateData,
    double LatitudeDegrees,
    double LongitudeDegrees,
    TimeSpan TimeZoneOffset,
    IReadOnlyList<WeatherSolarSurface>? Surfaces = null,
    double GroundReflectance = 0.2,
    Iso52016GroundBoundaryTemperatureOptions? GroundBoundaryTemperature = null);