namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record DistributionCircuitModel(
    double? Efficiency = null,
    double? LossFactor = null,
    double AuxiliaryEnergyFraction = 0.0);
