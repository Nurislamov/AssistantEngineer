using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

internal static class Iso52016MultiZoneCouplingBuilder
{
    internal static IReadOnlyList<CouplingLink> BuildInterZoneCouplingLinks(
        IReadOnlyList<ThermalZoneBoundaryLink> boundaryLinks,
        IReadOnlyList<InterZoneConductanceLink> interZoneConductanceLinks,
        IReadOnlyDictionary<string, int> zoneIndexById)
    {
        var links = new List<CouplingLink>();

        foreach (var boundaryLink in boundaryLinks.Where(link => link.BoundaryType == MultiZoneBoundaryLinkType.InterZoneBoundary))
        {
            if (string.IsNullOrWhiteSpace(boundaryLink.TargetZoneId))
                continue;
            if (!zoneIndexById.TryGetValue(boundaryLink.SourceZoneId, out var fromIndex))
                continue;
            if (!zoneIndexById.TryGetValue(boundaryLink.TargetZoneId, out var toIndex))
                continue;
            if (fromIndex == toIndex)
                continue;

            links.Add(new CouplingLink(fromIndex, toIndex, Math.Max(0.0, boundaryLink.ConductanceWPerK)));
        }

        foreach (var link in interZoneConductanceLinks)
        {
            if (!zoneIndexById.TryGetValue(link.FromZoneId, out var fromIndex))
                continue;
            if (!zoneIndexById.TryGetValue(link.ToZoneId, out var toIndex))
                continue;
            if (fromIndex == toIndex)
                continue;

            links.Add(new CouplingLink(fromIndex, toIndex, Math.Max(0.0, link.ConductanceWPerK)));
        }

        return links;
    }
}

internal readonly record struct CouplingLink(
    int FromIndex,
    int ToIndex,
    double ConductanceWPerK);
