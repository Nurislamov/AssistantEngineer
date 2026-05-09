using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

internal static class SystemEnergyFoundationMappingHelper
{
    public static SystemEnergyUseKind ToUseKind(SystemEnergyEndUse endUse) =>
        endUse switch
        {
            SystemEnergyEndUse.SpaceHeating => SystemEnergyUseKind.SpaceHeating,
            SystemEnergyEndUse.SpaceCooling => SystemEnergyUseKind.SpaceCooling,
            SystemEnergyEndUse.DomesticHotWater => SystemEnergyUseKind.DomesticHotWater,
            SystemEnergyEndUse.Ventilation => SystemEnergyUseKind.Ventilation,
            SystemEnergyEndUse.Auxiliary => SystemEnergyUseKind.Auxiliary,
            _ => SystemEnergyUseKind.Generic
        };

    public static SystemEnergyCarrierKind ToCarrierKind(SystemEnergyCarrier carrier) =>
        carrier switch
        {
            SystemEnergyCarrier.Electricity => SystemEnergyCarrierKind.Electricity,
            SystemEnergyCarrier.NaturalGas => SystemEnergyCarrierKind.NaturalGas,
            SystemEnergyCarrier.DistrictHeating => SystemEnergyCarrierKind.DistrictHeating,
            SystemEnergyCarrier.DistrictCooling => SystemEnergyCarrierKind.DistrictCooling,
            SystemEnergyCarrier.Biomass => SystemEnergyCarrierKind.Biomass,
            SystemEnergyCarrier.FuelOil => SystemEnergyCarrierKind.Oil,
            SystemEnergyCarrier.LPG => SystemEnergyCarrierKind.LPG,
            SystemEnergyCarrier.SolarThermal => SystemEnergyCarrierKind.SolarThermal,
            SystemEnergyCarrier.Other => SystemEnergyCarrierKind.Other,
            _ => SystemEnergyCarrierKind.Unknown
        };
}
