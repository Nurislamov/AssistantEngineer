namespace AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;

public sealed record HeatingSystemEnergyResult(
    double UsefulHeatingDemandKWh,
    double FinalHeatingEnergyKWh,
    double TotalSystemEfficiency,
    double GenerationLossKWh,
    double DistributionLossKWh,
    double EmissionLossKWh);