namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneMonthlySummary(
    int Month,
    IReadOnlyDictionary<string, double> HeatingEnergyByZoneKWh,
    IReadOnlyDictionary<string, double> CoolingEnergyByZoneKWh,
    double BuildingHeatingEnergyKWh,
    double BuildingCoolingEnergyKWh);
