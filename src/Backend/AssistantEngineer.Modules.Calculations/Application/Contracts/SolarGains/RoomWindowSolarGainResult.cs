namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;

public sealed record RoomWindowSolarGainResult(
    int RoomId,
    double TotalSolarGainW,
    IReadOnlyList<WindowSolarGainResult> WindowBreakdown,
    double? PeakSolarGainW,
    int? PeakHour,
    IReadOnlyList<SolarGainDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic =>
        diagnostic.Severity == SolarGainDiagnosticSeverity.Error);
}
