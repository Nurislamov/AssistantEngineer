using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

public sealed record RoomLoadCalculationResult(
    int RoomId,
    string? RoomCode,
    string? RoomName,
    double AreaM2,
    double HeatingLoadW,
    double CoolingLoadW,
    double HeatingLoadWPerM2,
    double CoolingLoadWPerM2,
    RoomHeatingLoadBreakdown HeatingBreakdown,
    RoomCoolingLoadBreakdown CoolingBreakdown,
    string DominantHeatingComponent,
    string DominantCoolingComponent,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    IReadOnlyList<string> AssumptionsUsed,
    string CalculationMethod,
    string CalculationVersion,
    DateTimeOffset CalculatedAtUtc)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}
