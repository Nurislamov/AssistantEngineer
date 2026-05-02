namespace AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;

public sealed record CoolingSystemEnergyResult(
    double UsefulCoolingDemandKWh,
    double DeliveredCoolingKWh,
    double CompressorElectricityKWh,
    double AuxiliaryElectricityKWh,
    double FinalCoolingElectricityKWh,
    double SeasonalCop,
    double DistributionLossKWh,
    double EmissionLossKWh);