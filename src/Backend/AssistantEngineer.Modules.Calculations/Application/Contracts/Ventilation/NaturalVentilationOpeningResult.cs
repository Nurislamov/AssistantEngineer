using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationOpeningResult(
    string OpeningId,
    string? RoomId,
    string? ZoneId,
    string? SurfaceId,
    double EffectiveOpeningAreaSquareMeters,
    double DischargeCoefficient,
    double? WindPressureDifferencePa,
    double? StackPressureDifferencePa,
    double? CombinedPressureDifferencePa,
    double? AirflowCubicMetersPerSecond,
    double? AirflowCubicMetersPerHour,
    double? AirflowKilogramsPerSecond,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
