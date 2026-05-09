namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public enum DomesticHotWaterUseCategory
{
    Unknown = 0,
    Residential = 1,
    Apartment = Residential,
    Office = 2,
    Hotel = 3,
    School = 4,
    Healthcare = 5,
    Restaurant = 6,
    SportsFacility = 7,
    Industrial = 8,
    Retail = 9,
    Other = 10,
    Generic = Other,
    Unsupported = 11
}
