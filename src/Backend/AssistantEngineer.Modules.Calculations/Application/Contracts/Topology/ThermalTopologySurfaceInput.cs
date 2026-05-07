namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologySurfaceInput(
    string SurfaceId,
    string? RoomId,
    string? ZoneId,
    ThermalBoundaryKind BoundaryKind,
    double AreaSquareMeters,
    double? UValueWPerSquareMeterKelvin,
    string? AdjacentZoneId,
    string? AdjacentRoomId,
    string? BoundarySource);
