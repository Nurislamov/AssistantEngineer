using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyCarrierPrimaryEnergyResult(
    SystemEnergyCarrier Carrier,
    IReadOnlyList<double> HourlyFinalEnergyKWh8760,
    IReadOnlyList<double> HourlyRenewablePrimaryEnergyKWh8760,
    IReadOnlyList<double> HourlyNonRenewablePrimaryEnergyKWh8760,
    IReadOnlyList<double> HourlyTotalPrimaryEnergyKWh8760,
    double AnnualFinalEnergyKWh,
    double AnnualRenewablePrimaryEnergyKWh,
    double AnnualNonRenewablePrimaryEnergyKWh,
    double AnnualTotalPrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyFinalEnergyKWh,
    IReadOnlyList<double> MonthlyRenewablePrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyNonRenewablePrimaryEnergyKWh,
    IReadOnlyList<double> MonthlyTotalPrimaryEnergyKWh,
    SystemEnergyPrimaryEnergyFactor Factor,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
