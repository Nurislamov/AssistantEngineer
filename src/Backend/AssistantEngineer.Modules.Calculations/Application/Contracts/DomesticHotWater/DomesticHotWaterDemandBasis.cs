namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public enum DomesticHotWaterDemandBasis
{
    Unknown = 0,
    People = 1,
    PerPerson = People,
    DwellingUnit = 2,
    PerDwelling = DwellingUnit,
    FloorArea = 3,
    PerFloorArea = FloorArea,
    FixtureUse = 4,
    PerFixture = FixtureUse,
    CustomDailyVolume = 5,
    CustomHourlyVolume = 6,
    ScheduledVolume = CustomHourlyVolume,
    Other = 7,
    Custom = Other,
    ScheduledEnergy = 8
}
