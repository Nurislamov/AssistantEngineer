namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public enum DomesticHotWaterDemandBasis
{
    Unknown = 0,
    People = 1,
    DwellingUnit = 2,
    FloorArea = 3,
    FixtureUse = 4,
    CustomDailyVolume = 5,
    CustomHourlyVolume = 6,
    Other = 7
}
