using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundSurfaceBoundaryCalculationResult(
    string SurfaceId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    GroundContactKind ContactKind,
    double? EquivalentUValueWPerSquareMeterKelvin,
    double? HeatTransferCoefficientWPerKelvin,
    IReadOnlyList<double> MonthlyGroundBoundaryTemperaturesCelsius,
    IReadOnlyList<double> HourlyGroundBoundaryTemperaturesCelsius,
    GroundBoundaryCalculationResult GroundResult,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
