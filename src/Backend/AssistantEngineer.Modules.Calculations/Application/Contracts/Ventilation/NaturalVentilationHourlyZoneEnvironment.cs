using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationHourlyZoneEnvironment(
    int HourIndex,
    string? RoomId,
    string? ZoneId,
    double IndoorTemperatureCelsius,
    double OutdoorTemperatureCelsius,
    double WindSpeedMetersPerSecond,
    double? OccupancyFraction,
    double? ScheduleFraction,
    bool IsNightHour,
    double? AirDensityKgPerCubicMeter,
    double? AirSpecificHeatJPerKgKelvin,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
