namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record GenerationSystemModel(
    En15316GenerationTechnology Technology,
    En15316EnergyCarrier Carrier,
    double? Efficiency = null,
    double? Cop = null,
    double PrimaryEnergyFactor = 1.0);
