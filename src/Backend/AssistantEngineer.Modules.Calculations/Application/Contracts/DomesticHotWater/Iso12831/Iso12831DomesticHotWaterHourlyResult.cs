namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

public sealed record Iso12831DomesticHotWaterHourlyResult(
    int HourOfYear,
    int Month,
    double VolumeLiters,
    double DrawEnergyKWh,
    double TotalEnergyKWh);
