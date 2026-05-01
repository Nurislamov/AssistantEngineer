using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;

public sealed record RoomWindowSolarGainResult(
    int RoomId,
    double TotalSolarGainW,
    IReadOnlyList<WindowSolarGainResult> WindowBreakdown,
    double? PeakSolarGainW,
    int? PeakHour,
    IReadOnlyList<CalculationDiagnostic> Diagnostics)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}