using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Buildings;

internal static class FloorSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<FloorResponse>> ByField =
        new Dictionary<string, SortRule<FloorResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, floor => floor.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, floor => floor.Name)
                    .ThenByStable(descending, floor => floor.Id)
        };
}