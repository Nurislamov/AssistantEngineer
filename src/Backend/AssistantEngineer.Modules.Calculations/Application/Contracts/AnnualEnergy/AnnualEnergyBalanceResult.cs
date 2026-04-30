using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;

public sealed record AnnualEnergyBalanceResult(
    int BuildingId,
    string? BuildingName,
    int Year,
    double AnnualHeatingDemandKWh,
    double AnnualCoolingDemandKWh,
    double AnnualTotalDemandKWh,
    double EnergyUseIntensityKWhPerM2Year,
    IReadOnlyList<AnnualEnergyMonthlyResult> MonthlyResults,
    double PeakHeatingLoadW,
    double PeakCoolingLoadW,
    int? PeakHeatingHour,
    int? PeakCoolingHour,
    AnnualEnergyComponentBreakdown ComponentBreakdown,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    IReadOnlyList<string> AssumptionsUsed,
    string CalculationMethod,
    string CalculationVersion,
    DateTimeOffset CalculatedAtUtc)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}
