using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record BuildingThermalBoundaryCalculationResult(
    string BuildingId,
    double TotalHeatTransferCoefficientWPerKelvin,
    double OutdoorHeatTransferCoefficientWPerKelvin,
    double GroundHeatTransferCoefficientWPerKelvin,
    double AdjacentConditionedHeatTransferCoefficientWPerKelvin,
    double AdjacentUnconditionedHeatTransferCoefficientWPerKelvin,
    double InternalPartitionHeatTransferCoefficientWPerKelvin,
    double AdiabaticAreaSquareMeters,
    IReadOnlyList<ThermalZoneBoundaryCalculationResult> Zones,
    IReadOnlyList<ThermalRoomBoundaryCalculationResult> UnassignedRooms,
    IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> UnassignedSurfaces,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
