using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterReferenceDataProvider
{
    public Iso12831DomesticHotWaterReferenceDefaults Resolve(
        Iso12831DomesticHotWaterUsageCategory usageCategory)
    {
        return usageCategory switch
        {
            Iso12831DomesticHotWaterUsageCategory.ResidentialApartment => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 45.0,
                LitersPerM2Day: 1.2,
                LitersPerUnitDay: 120.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend),
            Iso12831DomesticHotWaterUsageCategory.SingleFamilyHouse => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 50.0,
                LitersPerM2Day: 1.0,
                LitersPerUnitDay: 180.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend),
            Iso12831DomesticHotWaterUsageCategory.Office => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 15.0,
                LitersPerM2Day: 0.35,
                LitersPerUnitDay: 200.0,
                EquivalentOccupantFactor: 0.8,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.OfficeDaytime),
            Iso12831DomesticHotWaterUsageCategory.Hotel => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 65.0,
                LitersPerM2Day: 1.4,
                LitersPerUnitDay: 220.0,
                EquivalentOccupantFactor: 1.1,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.HotelMorningEvening),
            Iso12831DomesticHotWaterUsageCategory.School => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 12.0,
                LitersPerM2Day: 0.3,
                LitersPerUnitDay: 140.0,
                EquivalentOccupantFactor: 0.9,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.SchoolDaytime),
            Iso12831DomesticHotWaterUsageCategory.Healthcare => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 80.0,
                LitersPerM2Day: 1.1,
                LitersPerUnitDay: 260.0,
                EquivalentOccupantFactor: 1.2,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat),
            Iso12831DomesticHotWaterUsageCategory.Restaurant => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 25.0,
                LitersPerM2Day: 0.8,
                LitersPerUnitDay: 320.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat),
            _ => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 40.0,
                LitersPerM2Day: 0.6,
                LitersPerUnitDay: 180.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat)
        };
    }
}

public sealed record Iso12831DomesticHotWaterReferenceDefaults(
    double LitersPerPersonDay,
    double LitersPerM2Day,
    double LitersPerUnitDay,
    double EquivalentOccupantFactor,
    Iso12831DomesticHotWaterDrawProfileKind DrawProfileKind);
