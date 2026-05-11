using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

internal static class Iso52016MultiZoneBoundaryResolver
{
    internal static bool TryResolveWeightedExternalTemperature(
        string zoneId,
        int hourOfYear,
        IReadOnlyDictionary<string, ThermalZoneBoundaryLink[]> boundaryLinksByZone,
        IReadOnlyDictionary<string, MultiZoneHourlyBoundaryCondition> boundaryConditionsById,
        out double temperatureCelsius)
    {
        temperatureCelsius = 0.0;

        if (!boundaryLinksByZone.TryGetValue(zoneId, out var links))
            return false;

        var weightedSum = 0.0;
        var totalConductance = 0.0;
        foreach (var link in links.Where(link => link.BoundaryType == MultiZoneBoundaryLinkType.ExternalBoundary))
        {
            if (!TryResolveBoundaryTemperature(link.SourceBoundaryId, hourOfYear, boundaryConditionsById, out var boundaryTemperature))
                continue;

            var conductance = Math.Max(0.0, link.ConductanceWPerK);
            weightedSum += conductance * boundaryTemperature;
            totalConductance += conductance;
        }

        if (totalConductance <= 0.0)
            return false;

        temperatureCelsius = weightedSum / totalConductance;
        return true;
    }

    internal static bool TryResolveBoundaryTemperature(
        string boundaryId,
        int hourOfYear,
        IReadOnlyDictionary<string, MultiZoneHourlyBoundaryCondition> boundaryConditionsById,
        out double temperatureCelsius)
    {
        temperatureCelsius = 0.0;
        if (string.IsNullOrWhiteSpace(boundaryId))
            return false;

        if (!boundaryConditionsById.TryGetValue(boundaryId, out var boundaryCondition))
            return false;

        temperatureCelsius = ResolveProfileValue(boundaryCondition.TemperatureProfileCelsius, hourOfYear);
        return true;
    }

    internal static double ResolveProfileValue(
        IReadOnlyList<double> profile,
        int hourOfYear)
    {
        if (profile.Count == 0)
            return 0.0;

        if (profile.Count == 1)
            return profile[0];

        if (hourOfYear >= 0 && hourOfYear < profile.Count)
            return profile[hourOfYear];

        return profile[^1];
    }

    internal static int ResolveHourCount(
        MultiZoneCalculationInput input,
        IReadOnlyDictionary<string, MultiZoneZoneHourlyProfile> zoneProfiles)
    {
        var counts = new List<int>();
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.HeatingSetpointProfileCelsius.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.CoolingSetpointProfileCelsius.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.InternalGainsProfileW.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.SolarGainsProfileW.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.VentilationInfiltrationConductanceProfileWPerK.Count));
        counts.AddRange((input.HourlyBoundaryConditions ?? []).Select(condition => condition.TemperatureProfileCelsius.Count));
        counts.AddRange(input.BoundaryLinks
            .Where(link => link.AdjacentBoundaryCondition?.TemperatureProfileCelsius is not null)
            .Select(link => link.AdjacentBoundaryCondition!.TemperatureProfileCelsius.Count));

        if (counts.Count == 0)
            return 1;

        return counts.Max();
    }
}
