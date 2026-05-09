namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

public sealed record DomesticHotWaterReferenceTableEntry(
    string EntryId,
    DomesticHotWaterUsageCategory UsageCategory,
    double LitersPerPersonDay,
    double LitersPerM2Day,
    double LitersPerUnitDay,
    double EquivalentOccupantFactor);
