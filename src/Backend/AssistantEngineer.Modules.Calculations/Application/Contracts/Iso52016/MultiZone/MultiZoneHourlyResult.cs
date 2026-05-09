namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneHourlyResult(
    int HourOfYear,
    IReadOnlyDictionary<string, double> ZoneTemperaturesCelsius,
    IReadOnlyDictionary<string, double> HeatingLoadsByZoneW,
    IReadOnlyDictionary<string, double> CoolingLoadsByZoneW,
    double BuildingHeatingLoadW,
    double BuildingCoolingLoadW);
