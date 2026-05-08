using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationOpeningControlRule(
    string RuleId,
    string? OpeningId,
    string? RoomId,
    string? ZoneId,
    NaturalVentilationControlMode ControlMode,
    NaturalVentilationNightVentilationMode NightVentilationMode,
    double? FixedOpeningFraction,
    double? MinimumOpeningFraction,
    double? MaximumOpeningFraction,
    double? IndoorTemperatureOpenAboveCelsius,
    double? IndoorTemperatureCloseBelowCelsius,
    double? OutdoorTemperatureMinimumCelsius,
    double? OutdoorTemperatureMaximumCelsius,
    double? IndoorOutdoorTemperatureDifferenceMinimumKelvin,
    bool? RequiresOccupancy,
    string? ScheduleId,
    string? OccupancyProfileId,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
