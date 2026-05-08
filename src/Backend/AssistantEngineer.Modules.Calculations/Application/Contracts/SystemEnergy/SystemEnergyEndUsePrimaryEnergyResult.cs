using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyEndUsePrimaryEnergyResult(
    SystemEnergyEndUse EndUse,
    IReadOnlyDictionary<SystemEnergyCarrier, IReadOnlyList<double>> HourlyFinalEnergyByCarrierKWh8760,
    IReadOnlyDictionary<SystemEnergyCarrier, IReadOnlyList<double>> HourlyTotalPrimaryEnergyByCarrierKWh8760,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualFinalEnergyByCarrierKWh,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualTotalPrimaryEnergyByCarrierKWh,
    double AnnualFinalEnergyKWh,
    double AnnualRenewablePrimaryEnergyKWh,
    double AnnualNonRenewablePrimaryEnergyKWh,
    double AnnualTotalPrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyFinalEnergyKWh,
    IReadOnlyList<double> MonthlyRenewablePrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyNonRenewablePrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyTotalPrimaryEnergyKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
