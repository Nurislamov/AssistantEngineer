namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record En15316SystemEnergyResult(
    IReadOnlyList<En15316SystemEnergyEndUseResult> EndUses,
    IReadOnlyDictionary<En15316EndUse, double> FinalEnergyByEndUseKWh,
    IReadOnlyDictionary<En15316EnergyCarrier, double> FinalEnergyByCarrierKWh,
    IReadOnlyDictionary<En15316EnergyCarrier, double> PrimaryEnergyByCarrierKWh,
    IReadOnlyDictionary<En15316EnergyCarrier, double> RenewablePrimaryEnergyByCarrierKWh,
    IReadOnlyDictionary<En15316EnergyCarrier, double> NonRenewablePrimaryEnergyByCarrierKWh,
    double TotalFinalEnergyKWh,
    double TotalPrimaryEnergyKWh,
    IReadOnlyList<En15316SystemEnergyDiagnostics> Diagnostics,
    IReadOnlyList<string> AssumptionsUsed);
