namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

public sealed record Iso12831DomesticHotWaterResult(
    double DailyVolumeLiters,
    double DailyDrawEnergyKWh,
    double DailyTotalEnergyKWh,
    double AnnualVolumeLiters,
    double AnnualDrawEnergyKWh,
    double AnnualTotalEnergyKWh,
    IReadOnlyList<Iso12831DomesticHotWaterMonthlyResult> MonthlyResults,
    IReadOnlyList<Iso12831DomesticHotWaterHourlyResult> HourlyResults,
    double EquivalentOccupantsUsed,
    double ReferenceDailyVolumeLiters,
    IReadOnlyList<Iso12831DomesticHotWaterDiagnostics> Diagnostics,
    IReadOnlyList<string> AssumptionsUsed);
