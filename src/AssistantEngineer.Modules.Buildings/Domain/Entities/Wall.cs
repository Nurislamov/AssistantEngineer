using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Modules.Buildings.Domain.Entities;

public class Wall
{
    public int Id { get; private set; }
    public Area Area { get; private set; } = null!;
    public bool IsExternal { get; private set; }
    public ThermalTransmittance UValue { get; private set; } = null!;
    public CardinalDirection Orientation { get; private set; }
    public WallBoundaryType BoundaryType { get; private set; }

    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    public int? AdjacentRoomId { get; private set; }
    public Room? AdjacentRoom { get; private set; }

    public int? ConstructionAssemblyId { get; private set; }
    public ConstructionAssembly? ConstructionAssembly { get; private set; }

    private Wall() { }

    private Wall(
        Area area,
        ThermalTransmittance uValue,
        CardinalDirection orientation,
        WallBoundaryType boundaryType,
        Room room,
        Room? adjacentRoom)
    {
        Area = area;
        UValue = uValue;
        Orientation = orientation;
        BoundaryType = boundaryType;
        IsExternal = boundaryType == WallBoundaryType.External;
        Room = room;
        RoomId = room.Id;
        AdjacentRoom = adjacentRoom;
        AdjacentRoomId = adjacentRoom?.Id;
    }

    public static Result<Wall> Create(
        Area area,
        ThermalTransmittance uValue,
        CardinalDirection orientation,
        WallBoundaryType boundaryType,
        Room room,
        Room? adjacentRoom = null)
    {
        if (boundaryType is WallBoundaryType.AdjacentConditioned or WallBoundaryType.AdjacentUnconditioned)
        {
            if (adjacentRoom is null)
                return Result<Wall>.Validation("Adjacent room is required for adjacent wall boundary types.");

            if (adjacentRoom == room || adjacentRoom.Id == room.Id)
                return Result<Wall>.Validation("A wall cannot reference the same room as its adjacent room.");
        }
        else if (adjacentRoom is not null)
        {
            return Result<Wall>.Validation("Adjacent room can only be specified for adjacent wall boundary types.");
        }

        return Result<Wall>.Success(new Wall(
            area,
            uValue,
            orientation,
            boundaryType,
            room,
            adjacentRoom));
    }

    public Result SetConstructionAssembly(ConstructionAssembly? constructionAssembly)
    {
        ConstructionAssembly = constructionAssembly;
        ConstructionAssemblyId = constructionAssembly?.Id;
        return Result.Success();
    }
    
    public Result ClearUnexpectedAdjacentRoomReference()
    {
        if (BoundaryType is WallBoundaryType.AdjacentConditioned or WallBoundaryType.AdjacentUnconditioned)
            return Result.Validation("Cannot clear adjacent room reference for an adjacent wall boundary type.");

        AdjacentRoom = null;
        AdjacentRoomId = null;
        return Result.Success();
    }
}