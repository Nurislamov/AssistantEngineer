using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Searching.Projects;

internal static class ProjectSearchExtensions
{
    public static IEnumerable<ProjectResponse> ApplyProjectSearch(
        this IEnumerable<ProjectResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            project => project.Name);
}