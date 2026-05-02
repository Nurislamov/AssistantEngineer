using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Filtering.Buildings;

internal static class RoomListFilters
{
    public static IEnumerable<RoomResponse> ApplyRoomFilters(
        this IEnumerable<RoomResponse> source,
        RoomListQueryParameters query) =>
        source
            .ApplyValueFilter<RoomResponse, int>(
                query.FloorId,
                room => room.FloorId)
            .ApplyValueFilter<RoomResponse, RoomTypeDto>(
                query.Type,
                room => room.Type);

    public static IEnumerable<WindowResponse> ApplyWindowFilters(
        this IEnumerable<WindowResponse> source,
        WindowListQueryParameters query) =>
        source.ApplyValueFilter<WindowResponse, CardinalDirectionDto>(
            query.Orientation,
            window => window.Orientation);

    public static IEnumerable<WallResponse> ApplyWallFilters(
        this IEnumerable<WallResponse> source,
        WallListQueryParameters query) =>
        source
            .ApplyValueFilter<WallResponse, CardinalDirectionDto>(
                query.Orientation,
                wall => wall.Orientation)
            .ApplyValueFilter<WallResponse, WallBoundaryTypeDto>(
                query.BoundaryType,
                wall => wall.BoundaryType)
            .ApplyValueFilter<WallResponse, bool>(
                query.IsExternal,
                wall => wall.IsExternal);
}