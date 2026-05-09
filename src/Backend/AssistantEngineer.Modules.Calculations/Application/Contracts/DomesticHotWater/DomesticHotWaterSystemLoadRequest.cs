namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterSystemLoadRequest(
    IReadOnlyList<double> UsefulDemandProfileKWh,
    DomesticHotWaterLossDefinition LossDefinition,
    IReadOnlyList<double>? ColdWaterTemperatureProfileCelsius,
    IReadOnlyList<double>? HotWaterSetpointProfileCelsius,
    double TimeStepHours);
