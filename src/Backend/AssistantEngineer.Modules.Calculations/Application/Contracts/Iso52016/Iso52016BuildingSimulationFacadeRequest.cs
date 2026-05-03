using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016BuildingSimulationFacadeRequest(
    string BuildingCode,
    IReadOnlyList<Room> Rooms,
    AnnualClimateData AnnualClimateData,
    double LatitudeDegrees,
    double LongitudeDegrees,
    TimeSpan TimeZoneOffset,
    IReadOnlyList<WeatherSolarSurface>? Surfaces = null,
    double GroundReflectance = 0.2,
    Iso52016GroundBoundaryTemperatureOptions? GroundBoundaryTemperature = null,
    Iso52016RoomSimulationDefaults? Defaults = null,
    double? HeatingSetpointOverrideC = null,
    double? CoolingSetpointOverrideC = null,
    Iso52016RoomHeatBalanceOptions? HeatBalanceOptions = null,
    Iso52016SimulationEngine SimulationEngine = Iso52016SimulationEngine.Matrix);