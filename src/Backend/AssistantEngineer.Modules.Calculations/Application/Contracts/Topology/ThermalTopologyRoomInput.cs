namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologyRoomInput(
    string RoomId,
    string? ZoneId,
    double? VolumeCubicMeters,
    double? FloorAreaSquareMeters);
