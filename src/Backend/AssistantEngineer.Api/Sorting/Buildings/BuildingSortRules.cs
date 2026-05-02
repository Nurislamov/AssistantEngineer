using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Buildings;

internal static class BuildingSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<BuildingResponse>> ByField =
        new Dictionary<string, SortRule<BuildingResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, building => building.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, building => building.Name)
                    .ThenByStable(descending, building => building.Id),

            ["climatezonename"] = (items, descending) =>
                items.SortBy(descending, building => building.ClimateZoneName)
                    .ThenByStable(descending, building => building.Id)
        };
}