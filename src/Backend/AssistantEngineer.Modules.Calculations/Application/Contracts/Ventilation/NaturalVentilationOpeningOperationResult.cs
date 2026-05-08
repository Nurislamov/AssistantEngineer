using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationOpeningOperationResult(
    string RuleId,
    string? OpeningId,
    string? RoomId,
    string? ZoneId,
    int HourIndex,
    NaturalVentilationControlMode ControlMode,
    double OpeningFraction,
    bool IsOpen,
    bool IsNightVentilationActive,
    IReadOnlyList<string> ActiveReasons,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
