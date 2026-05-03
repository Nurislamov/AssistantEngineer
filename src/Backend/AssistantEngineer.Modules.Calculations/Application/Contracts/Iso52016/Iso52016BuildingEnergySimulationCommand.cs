using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016BuildingEnergySimulationCommand(
    double LatitudeDegrees,
    double LongitudeDegrees,
    TimeSpan TimeZoneOffset,
    int? WeatherYear = null,
    IReadOnlyList<WeatherSolarSurface>? Surfaces = null,
    double GroundReflectance = 0.2,
    Iso52016GroundBoundaryTemperatureOptions? GroundBoundaryTemperature = null,
    Iso52016RoomSimulationDefaults? Defaults = null,
    double? HeatingSetpointOverrideC = null,
    double? CoolingSetpointOverrideC = null,
    Iso52016RoomHeatBalanceOptions? HeatBalanceOptions = null);