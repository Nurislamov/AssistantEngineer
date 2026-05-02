namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDemandResult(
    double DailyVolumeLiters,
    double DailyEnergyKWh,
    IReadOnlyList<DomesticHotWaterMonthlyDemand> MonthlyDemand,
    IReadOnlyList<DomesticHotWaterHourlyDemand> HourlyDemand,
    double AnnualVolumeLiters,
    double AnnualEnergyKWh,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> AssumptionsUsed);

public sealed record DomesticHotWaterMonthlyDemand(
    int Month,
    double VolumeLiters,
    double EnergyKWh);

public sealed record DomesticHotWaterHourlyDemand(
    int HourOfYear,
    int Month,
    double VolumeLiters,
    double EnergyKWh);
