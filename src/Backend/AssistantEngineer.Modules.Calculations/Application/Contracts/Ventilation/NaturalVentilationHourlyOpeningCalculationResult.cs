using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationHourlyOpeningCalculationResult(
    int HourIndex,
    string OpeningId,
    string? RoomId,
    string? ZoneId,
    double OpeningFraction,
    double AirflowCubicMetersPerSecond,
    double AirflowCubicMetersPerHour,
    double AirflowKilogramsPerSecond,
    double? VentilationHeatTransferCoefficientWPerKelvin,
    double? SensibleVentilationLoadWatts,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
