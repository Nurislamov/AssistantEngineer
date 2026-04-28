using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomEnergySimulationBuildRequest(
    Room Room,
    Iso52016WeatherSolarContext WeatherSolarContext,
    Iso52016RoomSimulationDefaults? Defaults = null,
    double? HeatingSetpointOverrideC = null,
    double? CoolingSetpointOverrideC = null);