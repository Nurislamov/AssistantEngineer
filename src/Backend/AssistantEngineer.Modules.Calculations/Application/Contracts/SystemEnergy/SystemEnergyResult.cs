using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyResult(
    double UsefulHeatingKWh,
    double UsefulCoolingKWh,
    double UsefulDhwKWh,
    double FinalHeatingEnergyKWh,
    double FinalCoolingEnergyKWh,
    double FinalDhwEnergyKWh,
    double FinalFanEnergyKWh,
    double TotalFinalEnergyKWh,
    double? PrimaryEnergyKWh,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    IReadOnlyList<string> AssumptionsUsed)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}
