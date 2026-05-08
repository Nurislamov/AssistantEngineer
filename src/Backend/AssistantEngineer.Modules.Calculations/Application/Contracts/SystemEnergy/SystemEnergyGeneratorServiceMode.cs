namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyGeneratorServiceMode
{
    Unknown = 0,
    Heating = 1,
    Cooling = 2,
    DomesticHotWater = 3,
    HeatingAndDhw = 4,
    CoolingAndVentilation = 5,
    Auxiliary = 6,
    Other = 7
}
