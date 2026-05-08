using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyCalculationSummary(
    string CalculationId,
    double AnnualTotalFinalEnergyKWh,
    double AnnualTotalRenewablePrimaryEnergyKWh,
    double AnnualTotalNonRenewablePrimaryEnergyKWh,
    double AnnualTotalPrimaryEnergyKWh,
    double? AnnualTotalEmissionsKg,
    IReadOnlyList<SystemEnergyCarrierSummary> Carriers,
    IReadOnlyList<SystemEnergyEndUseSummary> EndUses,
    SystemEnergyDisclosureSummary DisclosureSummary,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
