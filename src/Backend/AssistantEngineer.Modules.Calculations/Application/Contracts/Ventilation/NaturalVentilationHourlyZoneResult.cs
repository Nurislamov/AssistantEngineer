using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationHourlyZoneResult(
    int HourIndex,
    string ZoneId,
    double TotalAirflowCubicMetersPerSecond,
    double TotalAirflowCubicMetersPerHour,
    double TotalAirflowKilogramsPerSecond,
    double? AirChangesPerHour,
    double VentilationHeatTransferCoefficientWPerKelvin,
    double SensibleVentilationLoadWatts,
    IReadOnlyList<NaturalVentilationHourlyRoomResult> Rooms,
    IReadOnlyList<NaturalVentilationHourlyOpeningCalculationResult> UnassignedOpenings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
