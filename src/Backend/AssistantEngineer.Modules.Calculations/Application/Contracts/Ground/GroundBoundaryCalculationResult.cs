using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundBoundaryCalculationResult(
    string BoundaryId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    string? SurfaceId,
    GroundContactKind ContactKind,
    double? EquivalentUValueWPerSquareMeterKelvin,
    double? HeatTransferCoefficientWPerKelvin,
    double? CharacteristicDimensionMeters,
    IReadOnlyList<double> MonthlyGroundBoundaryTemperaturesCelsius,
    IReadOnlyList<double> HourlyGroundBoundaryTemperaturesCelsius,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
