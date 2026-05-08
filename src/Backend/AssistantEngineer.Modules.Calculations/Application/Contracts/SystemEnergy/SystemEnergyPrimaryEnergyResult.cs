using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyPrimaryEnergyResult(
    string CalculationId,
    SystemEnergyFinalEnergyResult FinalEnergyResult,
    SystemEnergyFactorSet FactorSet,
    IReadOnlyList<SystemEnergyCarrierPrimaryEnergyResult> Carriers,
    IReadOnlyList<SystemEnergyEndUsePrimaryEnergyResult> EndUses,
    IReadOnlyList<SystemEnergyEmissionResult> Emissions,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualFinalEnergyByCarrierKWh,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualRenewablePrimaryEnergyByCarrierKWh,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualNonRenewablePrimaryEnergyByCarrierKWh,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualTotalPrimaryEnergyByCarrierKWh,
    double AnnualTotalFinalEnergyKWh,
    double AnnualTotalRenewablePrimaryEnergyKWh,
    double AnnualTotalNonRenewablePrimaryEnergyKWh,
    double AnnualTotalPrimaryEnergyKWh,
    double? AnnualTotalEmissionsKg,
    IReadOnlyList<double> MonthlyTotalFinalEnergyKWh,
    IReadOnlyList<double> MonthlyTotalRenewablePrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyTotalNonRenewablePrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyTotalPrimaryEnergyKWh,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
