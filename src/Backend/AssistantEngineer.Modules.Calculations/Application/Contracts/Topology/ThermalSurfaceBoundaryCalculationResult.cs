using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalSurfaceBoundaryCalculationResult(
    string SurfaceId,
    string? RoomId,
    string? ZoneId,
    ThermalBoundaryKind BoundaryKind,
    double AreaSquareMeters,
    double? UValueWPerSquareMeterKelvin,
    double? HeatTransferCoefficientWPerKelvin,
    double? BoundaryTemperatureCelsius,
    double? SourceZoneTemperatureCelsius,
    double? AdjacentTemperatureCelsius,
    bool IsHeatTransferBoundary,
    bool IsAdiabatic,
    bool IsResolved,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
