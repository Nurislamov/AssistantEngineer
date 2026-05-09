namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

public sealed record DomesticHotWaterUsageProfileSet(
    DomesticHotWaterReferenceTableEntry ReferenceTableEntry,
    DomesticHotWaterDrawProfileTable DrawProfileTable,
    DomesticHotWaterTemperatureAssumptions TemperatureAssumptions);
