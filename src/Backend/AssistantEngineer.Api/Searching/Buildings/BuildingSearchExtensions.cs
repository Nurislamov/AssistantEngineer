using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Searching.Buildings;

internal static class BuildingSearchExtensions
{
    public static IEnumerable<BuildingResponse> ApplyBuildingSearch(
        this IEnumerable<BuildingResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            building => building.Name,
            building => building.ClimateZoneName);

    public static IEnumerable<BuildingArchetypeSummary> ApplyBuildingArchetypeSearch(
        this IEnumerable<BuildingArchetypeSummary> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            archetype => archetype.Code,
            archetype => archetype.DisplayName,
            archetype => archetype.Type.ToString());

    public static IEnumerable<FloorResponse> ApplyFloorSearch(
        this IEnumerable<FloorResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            floor => floor.Name);

    public static IEnumerable<RoomResponse> ApplyRoomSearch(
        this IEnumerable<RoomResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            room => room.Name,
            room => room.Type.ToString());

    public static IEnumerable<WindowResponse> ApplyWindowSearch(
        this IEnumerable<WindowResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            window => window.Orientation.ToString(),
            window => window.Id.ToString());

    public static IEnumerable<WallResponse> ApplyWallSearch(
        this IEnumerable<WallResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            wall => wall.Orientation.ToString(),
            wall => wall.BoundaryType.ToString(),
            wall => wall.Id.ToString());

    public static IEnumerable<ThermalZoneResponse> ApplyThermalZoneSearch(
        this IEnumerable<ThermalZoneResponse> source,
        CollectionQueryParameters query) =>
        source.ApplySearch(
            query.Search,
            zone => zone.Name,
            zone => string.Join(
                " ",
                zone.Rooms.Select(room => room.Name)));
}