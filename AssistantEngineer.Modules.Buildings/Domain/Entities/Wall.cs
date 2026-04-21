using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Construction;
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

    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;
    
    public int? ConstructionAssemblyId { get; private set; }
    public ConstructionAssembly? ConstructionAssembly { get; private set; }

    private Wall() { }

    private Wall(Area area, bool isExternal, ThermalTransmittance uValue, CardinalDirection orientation, Room room)
    {
        Area = area;
        IsExternal = isExternal;
        UValue = uValue;
        Orientation = orientation;
        Room = room;
        RoomId = room.Id;
    }

    public static Result<Wall> Create(
        Area area,
        bool isExternal,
        ThermalTransmittance uValue,
        CardinalDirection orientation,
        Room room)
    {
        return Result<Wall>.Success(new Wall(area, isExternal, uValue, orientation, room));
    }

    public Result SetConstructionAssembly(ConstructionAssembly? constructionAssembly)
    {
        ConstructionAssembly = constructionAssembly;
        ConstructionAssemblyId = constructionAssembly?.Id;
        return Result.Success();
    }
}
