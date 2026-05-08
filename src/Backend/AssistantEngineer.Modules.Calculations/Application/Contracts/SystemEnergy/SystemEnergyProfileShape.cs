namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyProfileShape
{
    Unknown = 0,
    Hourly8760 = 1,
    Monthly12 = 2,
    AnnualScalar = 3,
    NonStandard = 4
}
