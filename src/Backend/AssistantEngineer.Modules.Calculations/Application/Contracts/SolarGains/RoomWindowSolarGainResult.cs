using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;

public sealed record RoomWindowSolarGainResult(
    int RoomId,
    double TotalSolarGainW,
    IReadOnlyList<WindowSolarGainResult> WindowBreakdown,
    double? PeakSolarGainW,
    int? PeakHourOfYear,
    IReadOnlyList<CalculationDiagnostic> Diagnostics)
{
    [Obsolete("Use PeakHourOfYear.")]
    public int? PeakHour => PeakHourOfYear;

    public bool HasErrors =>
        Diagnostics.Any(diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}
