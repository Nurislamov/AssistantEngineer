using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;

public sealed record AggregationRoomLoadInput(
    int RoomId,
    string RoomName,
    int? ThermalZoneId,
    int? FloorId,
    int BuildingId,
    double AreaM2,
    double HeatingLoadW,
    double CoolingLoadW,
    RoomHeatingLoadBreakdown? HeatingBreakdown = null,
    RoomCoolingLoadBreakdown? CoolingBreakdown = null,
    IReadOnlyList<double>? HourlyHeatingLoadW = null,
    IReadOnlyList<double>? HourlyCoolingLoadW = null);
