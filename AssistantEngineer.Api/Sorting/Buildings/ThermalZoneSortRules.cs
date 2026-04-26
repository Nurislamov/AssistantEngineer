using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Buildings;

internal static class ThermalZoneSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<ThermalZoneResponse>> ByField =
        new Dictionary<string, SortRule<ThermalZoneResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, zone => zone.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, zone => zone.Name)
                    .ThenByStable(descending, zone => zone.Id),

            ["roomscount"] = (items, descending) =>
                items.SortBy(descending, zone => zone.Rooms.Count)
                    .ThenByStable(descending, zone => zone.Id)
        };
}