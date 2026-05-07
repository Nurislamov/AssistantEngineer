using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalBoundaryResolutionResult(
    bool IsResolved,
    ThermalBoundaryKind BoundaryKind,
    string? SourceZoneId,
    string? SourceRoomId,
    string? AdjacentZoneId,
    string? AdjacentRoomId,
    bool IsHeatTransferBoundary,
    bool RequiresOutdoorTemperature,
    bool RequiresGroundTemperature,
    bool RequiresAdjacentZoneTemperature,
    bool RequiresAdjacentUnconditionedTemperature,
    bool IsAdiabatic,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
