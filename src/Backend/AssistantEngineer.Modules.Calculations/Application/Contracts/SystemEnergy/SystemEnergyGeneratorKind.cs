namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyGeneratorKind
{
    Unknown = 0,
    Boiler = 1,
    CondensingBoiler = 2,
    ElectricResistance = 3,
    HeatPump = 4,
    Chiller = 5,
    DistrictHeating = 6,
    DistrictCooling = 7,
    BiomassBoiler = 8,
    FuelOilBoiler = 9,
    LpgBoiler = 10,
    SolarThermal = 11,
    Custom = 12,
    Other = 13,
    GasBoiler = 14,
    SolarThermalContribution = 15,
    GenericEfficiencyGenerator = 16
}
