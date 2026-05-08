using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyCarrierSummary(
    SystemEnergyCarrier Carrier,
    double AnnualFinalEnergyKWh,
    double AnnualRenewablePrimaryEnergyKWh,
    double AnnualNonRenewablePrimaryEnergyKWh,
    double AnnualTotalPrimaryEnergyKWh,
    double? AnnualEmissionsKg,
    IReadOnlyList<double> MonthlyFinalEnergyKWh,
    IReadOnlyList<double> MonthlyTotalPrimaryEnergyKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
