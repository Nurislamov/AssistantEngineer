using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Buildings.Application.Mappers;

public static class BuildingsContractEnumMapper
{
    public static RoomType ToDomain(this RoomTypeDto type) =>
        type switch
        {
            RoomTypeDto.Office => RoomType.Office,
            RoomTypeDto.MeetingRoom => RoomType.MeetingRoom,
            RoomTypeDto.Corridor => RoomType.Corridor,
            RoomTypeDto.ServerRoom => RoomType.ServerRoom,
            RoomTypeDto.Retail => RoomType.Retail,
            RoomTypeDto.Residential => RoomType.Residential,
            RoomTypeDto.Other => RoomType.Other,
            _ => throw UnsupportedEnumValue(type)
        };

    public static RoomTypeDto ToContract(this RoomType type) =>
        type switch
        {
            RoomType.Office => RoomTypeDto.Office,
            RoomType.MeetingRoom => RoomTypeDto.MeetingRoom,
            RoomType.Corridor => RoomTypeDto.Corridor,
            RoomType.ServerRoom => RoomTypeDto.ServerRoom,
            RoomType.Retail => RoomTypeDto.Retail,
            RoomType.Residential => RoomTypeDto.Residential,
            RoomType.Other => RoomTypeDto.Other,
            _ => throw UnsupportedEnumValue(type)
        };

    public static CardinalDirection ToDomain(this CardinalDirectionDto direction) =>
        direction switch
        {
            CardinalDirectionDto.North => CardinalDirection.North,
            CardinalDirectionDto.NorthEast => CardinalDirection.NorthEast,
            CardinalDirectionDto.East => CardinalDirection.East,
            CardinalDirectionDto.SouthEast => CardinalDirection.SouthEast,
            CardinalDirectionDto.South => CardinalDirection.South,
            CardinalDirectionDto.SouthWest => CardinalDirection.SouthWest,
            CardinalDirectionDto.West => CardinalDirection.West,
            CardinalDirectionDto.NorthWest => CardinalDirection.NorthWest,
            _ => throw UnsupportedEnumValue(direction)
        };

    public static CardinalDirectionDto ToContract(this CardinalDirection direction) =>
        direction switch
        {
            CardinalDirection.North => CardinalDirectionDto.North,
            CardinalDirection.NorthEast => CardinalDirectionDto.NorthEast,
            CardinalDirection.East => CardinalDirectionDto.East,
            CardinalDirection.SouthEast => CardinalDirectionDto.SouthEast,
            CardinalDirection.South => CardinalDirectionDto.South,
            CardinalDirection.SouthWest => CardinalDirectionDto.SouthWest,
            CardinalDirection.West => CardinalDirectionDto.West,
            CardinalDirection.NorthWest => CardinalDirectionDto.NorthWest,
            _ => throw UnsupportedEnumValue(direction)
        };

    public static WallBoundaryType ToDomain(this WallBoundaryTypeDto boundaryType) =>
        boundaryType switch
        {
            WallBoundaryTypeDto.External => WallBoundaryType.External,
            WallBoundaryTypeDto.Ground => WallBoundaryType.Ground,
            WallBoundaryTypeDto.Adiabatic => WallBoundaryType.Adiabatic,
            WallBoundaryTypeDto.AdjacentConditioned => WallBoundaryType.AdjacentConditioned,
            WallBoundaryTypeDto.AdjacentUnconditioned => WallBoundaryType.AdjacentUnconditioned,
            _ => throw UnsupportedEnumValue(boundaryType)
        };

    public static WallBoundaryTypeDto ToContract(this WallBoundaryType boundaryType) =>
        boundaryType switch
        {
            WallBoundaryType.External => WallBoundaryTypeDto.External,
            WallBoundaryType.Ground => WallBoundaryTypeDto.Ground,
            WallBoundaryType.Adiabatic => WallBoundaryTypeDto.Adiabatic,
            WallBoundaryType.AdjacentConditioned => WallBoundaryTypeDto.AdjacentConditioned,
            WallBoundaryType.AdjacentUnconditioned => WallBoundaryTypeDto.AdjacentUnconditioned,
            _ => throw UnsupportedEnumValue(boundaryType)
        };

    private static ArgumentOutOfRangeException UnsupportedEnumValue<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        new(nameof(value), value, $"Unsupported {typeof(TEnum).Name} value.");
}