using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationHourlyRoomResult(
    int HourIndex,
    string RoomId,
    string? ZoneId,
    double TotalAirflowCubicMetersPerSecond,
    double TotalAirflowCubicMetersPerHour,
    double TotalAirflowKilogramsPerSecond,
    double? AirChangesPerHour,
    double VentilationHeatTransferCoefficientWPerKelvin,
    double SensibleVentilationLoadWatts,
    IReadOnlyList<NaturalVentilationHourlyOpeningCalculationResult> Openings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
