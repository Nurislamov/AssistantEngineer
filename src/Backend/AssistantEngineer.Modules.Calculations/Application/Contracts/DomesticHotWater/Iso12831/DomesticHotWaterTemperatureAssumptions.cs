namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

public sealed record DomesticHotWaterTemperatureAssumptions(
    double DefaultHotWaterTemperatureC,
    double DefaultColdWaterTemperatureC,
    string Notes);
