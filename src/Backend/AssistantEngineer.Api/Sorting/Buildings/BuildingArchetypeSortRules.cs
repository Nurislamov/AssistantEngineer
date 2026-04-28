using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Buildings;

internal static class BuildingArchetypeSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<BuildingArchetypeSummary>> ByField =
        new Dictionary<string, SortRule<BuildingArchetypeSummary>>(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.Code),

            ["displayname"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.DisplayName)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["type"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.Type)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["roomscount"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.RoomsCount)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["roomaream2"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.RoomAreaM2)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["roomheightm"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.RoomHeightM)
                    .ThenByStable(descending, archetype => archetype.Code)
        };
}