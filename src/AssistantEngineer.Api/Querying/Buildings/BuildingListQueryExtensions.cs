using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Filtering.Buildings;
using AssistantEngineer.Api.Searching.Buildings;
using AssistantEngineer.Api.Sorting.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Querying.Buildings;

internal static class BuildingListQueryExtensions
{
    public static IEnumerable<BuildingResponse> ApplyBuildingListQuery(
        this IEnumerable<BuildingResponse> source,
        BuildingListQueryParameters query) =>
        source
            .ApplyBuildingFilters(query)
            .ApplyBuildingSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                BuildingSortRules.ByField);

    public static IEnumerable<BuildingArchetypeSummary> ApplyBuildingArchetypeListQuery(
        this IEnumerable<BuildingArchetypeSummary> source,
        BuildingArchetypeListQueryParameters query) =>
        source
            .ApplyBuildingArchetypeFilters(query)
            .ApplyBuildingArchetypeSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "code",
                BuildingArchetypeSortRules.ByField);

    public static IEnumerable<FloorResponse> ApplyFloorListQuery(
        this IEnumerable<FloorResponse> source,
        CollectionQueryParameters query) =>
        source
            .ApplyFloorSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                FloorSortRules.ByField);

    public static IEnumerable<RoomResponse> ApplyRoomListQuery(
        this IEnumerable<RoomResponse> source,
        RoomListQueryParameters query) =>
        source
            .ApplyRoomFilters(query)
            .ApplyRoomSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                RoomSortRules.ByField);

    public static IEnumerable<WindowResponse> ApplyWindowListQuery(
        this IEnumerable<WindowResponse> source,
        WindowListQueryParameters query) =>
        source
            .ApplyWindowFilters(query)
            .ApplyWindowSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                WindowSortRules.ByField);

    public static IEnumerable<WallResponse> ApplyWallListQuery(
        this IEnumerable<WallResponse> source,
        WallListQueryParameters query) =>
        source
            .ApplyWallFilters(query)
            .ApplyWallSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                WallSortRules.ByField);

    public static IEnumerable<ThermalZoneResponse> ApplyThermalZoneListQuery(
        this IEnumerable<ThermalZoneResponse> source,
        CollectionQueryParameters query) =>
        source
            .ApplyThermalZoneSearch(query)
            .ApplySort(
                query,
                defaultSortBy: "id",
                ThermalZoneSortRules.ByField);
}