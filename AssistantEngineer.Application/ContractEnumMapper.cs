using AssistantEngineer.Application.Contracts.Common;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application;

public static class ContractEnumMapper
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

    public static CoolingLoadCalculationMethod ToDomain(this CoolingLoadCalculationMethodDto method) =>
        method switch
        {
            CoolingLoadCalculationMethodDto.Simplified => CoolingLoadCalculationMethod.Simplified,
            CoolingLoadCalculationMethodDto.Iso52016 => CoolingLoadCalculationMethod.Iso52016,
            _ => throw UnsupportedEnumValue(method)
        };

    public static CoolingLoadCalculationMethodDto ToContract(this CoolingLoadCalculationMethod method) =>
        method switch
        {
            CoolingLoadCalculationMethod.Simplified => CoolingLoadCalculationMethodDto.Simplified,
            CoolingLoadCalculationMethod.Iso52016 => CoolingLoadCalculationMethodDto.Iso52016,
            _ => throw UnsupportedEnumValue(method)
        };

    public static HeatingLoadCalculationMethod ToDomain(this HeatingLoadCalculationMethodDto method) =>
        method switch
        {
            HeatingLoadCalculationMethodDto.En12831 => HeatingLoadCalculationMethod.En12831,
            _ => throw UnsupportedEnumValue(method)
        };

    public static HeatingLoadCalculationMethodDto ToContract(this HeatingLoadCalculationMethod method) =>
        method switch
        {
            HeatingLoadCalculationMethod.En12831 => HeatingLoadCalculationMethodDto.En12831,
            _ => throw UnsupportedEnumValue(method)
        };

    private static ArgumentOutOfRangeException UnsupportedEnumValue<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        new(nameof(value), value, $"Unsupported {typeof(TEnum).Name} value.");
}


