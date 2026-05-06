namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record En15316SystemEnergyEndUseResult(
    En15316EndUse EndUse,
    En15316EnergyCarrier EnergyCarrier,
    En15316GenerationTechnology GenerationTechnology,
    double UsefulEnergyKWh,
    En15316SystemEnergyModuleResult Emission,
    En15316SystemEnergyModuleResult Distribution,
    En15316SystemEnergyModuleResult Storage,
    double GenerationInputEnergyKWh,
    double GenerationLossesKWh,
    double AuxiliaryEnergyKWh,
    double RecoveredLossesKWh,
    double FinalEnergyKWh,
    double PrimaryEnergyKWh,
    double? RenewablePrimaryEnergyKWh,
    double? NonRenewablePrimaryEnergyKWh);
