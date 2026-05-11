using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

internal static class Iso52016MultiZoneResultAggregator
{
    internal static IReadOnlyDictionary<string, double> BuildAnnualHeatingByZoneKWh(
        IReadOnlyList<ThermalZoneNode> zones,
        IReadOnlyList<MultiZoneHourlyResult> hourlyResults) =>
        zones.ToDictionary(
            zone => zone.ZoneId,
            zone => hourlyResults.Sum(hour => hour.HeatingLoadsByZoneW[zone.ZoneId]) / 1000.0,
            StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyDictionary<string, double> BuildAnnualCoolingByZoneKWh(
        IReadOnlyList<ThermalZoneNode> zones,
        IReadOnlyList<MultiZoneHourlyResult> hourlyResults) =>
        zones.ToDictionary(
            zone => zone.ZoneId,
            zone => hourlyResults.Sum(hour => hour.CoolingLoadsByZoneW[zone.ZoneId]) / 1000.0,
            StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyList<MultiZoneMonthlySummary> BuildMonthlySummaries(
        IReadOnlyList<MultiZoneHourlyResult> hourlyResults,
        IReadOnlyList<ThermalZoneNode> zones)
    {
        return hourlyResults
            .GroupBy(hour => ResolveMonth(hour.HourOfYear, hourlyResults.Count))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var heatingByZone = zones.ToDictionary(
                    zone => zone.ZoneId,
                    zone => group.Sum(hour => hour.HeatingLoadsByZoneW[zone.ZoneId]) / 1000.0,
                    StringComparer.OrdinalIgnoreCase);
                var coolingByZone = zones.ToDictionary(
                    zone => zone.ZoneId,
                    zone => group.Sum(hour => hour.CoolingLoadsByZoneW[zone.ZoneId]) / 1000.0,
                    StringComparer.OrdinalIgnoreCase);

                return new MultiZoneMonthlySummary(
                    Month: group.Key,
                    HeatingEnergyByZoneKWh: heatingByZone,
                    CoolingEnergyByZoneKWh: coolingByZone,
                    BuildingHeatingEnergyKWh: group.Sum(hour => hour.BuildingHeatingLoadW) / 1000.0,
                    BuildingCoolingEnergyKWh: group.Sum(hour => hour.BuildingCoolingLoadW) / 1000.0);
            })
            .ToArray();
    }

    private static int ResolveMonth(
        int hourOfYear,
        int hourCount)
    {
        if (hourCount != 8760)
            return 1;

        var monthHours =
            new[] { 744, 672, 744, 720, 744, 720, 744, 744, 720, 744, 720, 744 };
        var cursor = 0;
        for (var month = 1; month <= monthHours.Length; month++)
        {
            cursor += monthHours[month - 1];
            if (hourOfYear < cursor)
                return month;
        }

        return 12;
    }
}
