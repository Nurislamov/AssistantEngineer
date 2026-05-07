using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalRoomBoundaryCalculationResult(
    string RoomId,
    string? ZoneId,
    double TotalHeatTransferCoefficientWPerKelvin,
    double OutdoorHeatTransferCoefficientWPerKelvin,
    double GroundHeatTransferCoefficientWPerKelvin,
    double AdjacentConditionedHeatTransferCoefficientWPerKelvin,
    double AdjacentUnconditionedHeatTransferCoefficientWPerKelvin,
    double InternalPartitionHeatTransferCoefficientWPerKelvin,
    double AdiabaticAreaSquareMeters,
    IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> Surfaces,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
