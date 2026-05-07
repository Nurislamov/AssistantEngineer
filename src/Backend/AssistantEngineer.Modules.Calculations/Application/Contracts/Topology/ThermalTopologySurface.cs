using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologySurface(
    string SurfaceId,
    string? RoomId,
    string? ZoneId,
    ThermalBoundaryKind BoundaryKind,
    double AreaSquareMeters,
    double? UValueWPerSquareMeterKelvin,
    string? AdjacentZoneId,
    string? AdjacentRoomId,
    string? BoundarySource,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
