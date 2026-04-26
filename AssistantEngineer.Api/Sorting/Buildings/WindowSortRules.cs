using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Buildings;

internal static class WindowSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<WindowResponse>> ByField =
        new Dictionary<string, SortRule<WindowResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, window => window.Id),

            ["aream2"] = (items, descending) =>
                items.SortBy(descending, window => window.AreaM2)
                    .ThenByStable(descending, window => window.Id),

            ["uvalue"] = (items, descending) =>
                items.SortBy(descending, window => window.UValue)
                    .ThenByStable(descending, window => window.Id),

            ["shgc"] = (items, descending) =>
                items.SortBy(descending, window => window.Shgc)
                    .ThenByStable(descending, window => window.Id),

            ["orientation"] = (items, descending) =>
                items.SortBy(descending, window => window.Orientation)
                    .ThenByStable(descending, window => window.Id)
        };
}