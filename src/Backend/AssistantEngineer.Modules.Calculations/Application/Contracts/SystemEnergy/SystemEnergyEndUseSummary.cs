using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyEndUseSummary(
    SystemEnergyEndUse EndUse,
    double AnnualFinalEnergyKWh,
    double AnnualRenewablePrimaryEnergyKWh,
    double AnnualNonRenewablePrimaryEnergyKWh,
    double AnnualTotalPrimaryEnergyKWh,
    double? AnnualEmissionsKg,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualFinalEnergyByCarrierKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
