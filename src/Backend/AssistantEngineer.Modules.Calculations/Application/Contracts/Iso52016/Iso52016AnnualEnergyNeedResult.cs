using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016AnnualEnergyNeedResult(
    int BuildingId,
    string BuildingName,
    int Year,
    IReadOnlyList<Iso52016HourlyEnergyNeed> HourlyResults,
    IReadOnlyList<Iso52016MonthlyEnergyNeed> MonthlyResults,
    double AnnualHeatingDemandKWh,
    double AnnualCoolingDemandKWh,
    Iso52016EnergyBalanceBreakdown Breakdown,
    IReadOnlyList<Iso52016ZoneHourlyEnergyNeed>? ZoneHourlyResults = null,
    IReadOnlyList<Iso52016RoomHourlyEnergyNeed>? RoomHourlyResults = null,
    IReadOnlyList<CalculationDiagnostic>? Diagnostics = null);
