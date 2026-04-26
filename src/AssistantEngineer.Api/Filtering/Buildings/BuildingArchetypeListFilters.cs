using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Api.Filtering.Buildings;

internal static class BuildingArchetypeListFilters
{
    public static IEnumerable<BuildingArchetypeSummary> ApplyBuildingArchetypeFilters(
        this IEnumerable<BuildingArchetypeSummary> source,
        BuildingArchetypeListQueryParameters query) =>
        source.ApplyValueFilter<BuildingArchetypeSummary, RoomType>(
            query.Type,
            archetype => archetype.Type);
}