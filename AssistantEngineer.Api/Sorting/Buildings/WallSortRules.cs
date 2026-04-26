using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Buildings;

internal static class WallSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<WallResponse>> ByField =
        new Dictionary<string, SortRule<WallResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, wall => wall.Id),

            ["aream2"] = (items, descending) =>
                items.SortBy(descending, wall => wall.AreaM2)
                    .ThenByStable(descending, wall => wall.Id),

            ["uvalue"] = (items, descending) =>
                items.SortBy(descending, wall => wall.UValue)
                    .ThenByStable(descending, wall => wall.Id),

            ["orientation"] = (items, descending) =>
                items.SortBy(descending, wall => wall.Orientation)
                    .ThenByStable(descending, wall => wall.Id),

            ["boundarytype"] = (items, descending) =>
                items.SortBy(descending, wall => wall.BoundaryType)
                    .ThenByStable(descending, wall => wall.Id),

            ["isexternal"] = (items, descending) =>
                items.SortBy(descending, wall => wall.IsExternal)
                    .ThenByStable(descending, wall => wall.Id)
        };
}