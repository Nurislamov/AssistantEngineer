using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal sealed record Iso52016HourlyWeatherContext(
    int Year,
    HourlyClimateData[] HourlyData);

internal sealed record Iso52016ThermalZoneGroup(
    string Name,
    IReadOnlyCollection<Room> Rooms);

internal sealed record Iso52016ThermalZoneState(
    double FloorAreaM2,
    double VolumeM3,
    double OutdoorBoundaryHeatTransferCoefficientWPerK,
    double GroundBoundaryHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK,
    double HeatingSetpointC,
    double CoolingSetpointC);

internal sealed record Iso52016AdjacentBoundaryContribution(
    double HeatTransferCoefficientWPerK,
    double BoundaryTemperatureWeightedHeatTransferW);

internal sealed record Iso52016RoomHourResult(
    int RoomId,
    Iso52016RoomHourlyEnergyNeed Hour);

internal sealed record Iso52016ZoneHourResult(
    string ZoneName,
    Iso52016HourlyEnergyNeed Hour,
    IReadOnlyCollection<Iso52016RoomHourResult> Rooms);

internal sealed record Iso52016HourlyBuildingCalculationContext(
    IReadOnlyList<Iso52016ThermalZoneGroup> Zones,
    IReadOnlyDictionary<string, Iso52016ThermalZoneState> ZoneStates,
    IReadOnlyDictionary<int, string> RoomZoneMap,
    Dictionary<int, double> PreviousRoomTemperatures);

internal sealed record Iso52016HourlyZoneCalculationContext(
    Iso52016ThermalZoneGroup Zone,
    Iso52016ThermalZoneState ZoneState,
    IReadOnlyDictionary<int, string> RoomZoneMap,
    Dictionary<int, double> PreviousRoomTemperatures);
