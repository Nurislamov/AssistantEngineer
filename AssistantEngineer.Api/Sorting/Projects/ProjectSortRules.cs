using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Projects;

internal static class ProjectSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<ProjectResponse>> ByField =
        new Dictionary<string, SortRule<ProjectResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, project => project.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, project => project.Name)
                    .ThenByStable(descending, project => project.Id)
        };
}