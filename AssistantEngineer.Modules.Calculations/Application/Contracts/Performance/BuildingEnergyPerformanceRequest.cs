using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;

public sealed class BuildingEnergyPerformanceRequest
{
    public HeatingSystemEnergyRequest HeatingSystem { get; set; } = new();
    public CoolingSystemEnergyRequest CoolingSystem { get; set; } = new();
    public EnergyCarrierType HeatingCarrier { get; set; } = EnergyCarrierType.NaturalGas;
    public EnergyCarrierType CoolingCarrier { get; set; } = EnergyCarrierType.Electricity;
    public bool IncludeDomesticHotWater { get; set; }
    public DomesticHotWaterDemandRequest? DomesticHotWater { get; set; }
    public DomesticHotWaterSystemRequest DomesticHotWaterSystem { get; set; } = new();
    public EnergyCarrierType DomesticHotWaterCarrier { get; set; } = EnergyCarrierType.NaturalGas;
    public Dictionary<EnergyCarrierType, EnergyCarrierFactors>? CarrierFactorOverrides { get; set; }
}