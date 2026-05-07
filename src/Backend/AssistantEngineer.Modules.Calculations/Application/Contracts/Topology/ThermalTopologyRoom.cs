using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologyRoom(
    string RoomId,
    string? ZoneId,
    double? VolumeCubicMeters,
    double? FloorAreaSquareMeters,
    IReadOnlyList<ThermalTopologySurface> Surfaces,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
