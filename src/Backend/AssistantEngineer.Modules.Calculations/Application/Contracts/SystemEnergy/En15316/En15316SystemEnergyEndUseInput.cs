namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record En15316SystemEnergyEndUseInput(
    En15316EndUse EndUse,
    En15316EnergyCarrier EnergyCarrier,
    En15316GenerationTechnology GenerationTechnology,
    double UsefulEnergyKWh,
    En15316SystemEnergyModuleInput Emission,
    En15316SystemEnergyModuleInput Distribution,
    En15316SystemEnergyModuleInput Storage,
    double? GenerationEfficiency = null,
    double? GenerationCop = null,
    double AuxiliaryEnergyKWh = 0,
    double RecoveredLossFraction = 0,
    double? PrimaryEnergyFactor = null,
    double? RenewablePrimaryEnergyFactor = null,
    double? NonRenewablePrimaryEnergyFactor = null,
    string? DiagnosticsContext = null);
