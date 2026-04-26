using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Filtering.Buildings;

internal static class BuildingListFilters
{
    public static IEnumerable<BuildingResponse> ApplyBuildingFilters(
        this IEnumerable<BuildingResponse> source,
        BuildingListQueryParameters query) =>
        source.ApplyNullableValueFilter<BuildingResponse, int>(
            query.ClimateZoneId,
            building => building.ClimateZoneId);
}