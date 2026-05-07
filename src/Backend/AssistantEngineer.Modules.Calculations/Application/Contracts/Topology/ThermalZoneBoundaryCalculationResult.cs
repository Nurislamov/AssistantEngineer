using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalZoneBoundaryCalculationResult(
    string ZoneId,
    string? Name,
    double TotalHeatTransferCoefficientWPerKelvin,
    double OutdoorHeatTransferCoefficientWPerKelvin,
    double GroundHeatTransferCoefficientWPerKelvin,
    double AdjacentConditionedHeatTransferCoefficientWPerKelvin,
    double AdjacentUnconditionedHeatTransferCoefficientWPerKelvin,
    double InternalPartitionHeatTransferCoefficientWPerKelvin,
    double AdiabaticAreaSquareMeters,
    IReadOnlyList<ThermalRoomBoundaryCalculationResult> Rooms,
    IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> UnassignedSurfaces,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
