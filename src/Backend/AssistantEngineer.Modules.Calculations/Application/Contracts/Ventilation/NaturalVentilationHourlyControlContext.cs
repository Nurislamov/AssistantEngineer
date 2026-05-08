using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationHourlyControlContext(
    int HourIndex,
    double IndoorTemperatureCelsius,
    double OutdoorTemperatureCelsius,
    double? WindSpeedMetersPerSecond,
    double? OccupancyFraction,
    double? ScheduleFraction,
    bool IsNightHour,
    string? RoomId,
    string? ZoneId,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
