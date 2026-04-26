using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Searching.Projects;
using AssistantEngineer.Api.Sorting.Projects;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Querying.Projects;

internal static class ProjectListQueryExtensions
{
    public static IEnumerable<ProjectResponse> ApplyProjectListQuery(
        this IEnumerable<ProjectResponse> source,
        CollectionQueryParameters query) =>
        source
            .ApplyProjectSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                ProjectSortRules.ByField);
}