using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Buildings.Application.Options;

public sealed class BuildingArchetypeCatalogOptions
{
    public int FormatVersion { get; init; } = 1;
    public List<BuildingArchetypeOptions> Archetypes { get; init; } = new();
}

public sealed class BuildingArchetypeOptions
{
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public RoomType Type { get; init; } = RoomType.Office;
    public int RoomsCount { get; init; }
    public double RoomAreaM2 { get; init; }
    public double RoomHeightM { get; init; }
    public double IndoorTemperatureC { get; init; }
    public int PeopleCount { get; init; }
    public double EquipmentLoadWPerM2 { get; init; }
    public double LightingLoadWPerM2 { get; init; }
    public double ExternalWallAreaFactor { get; init; }
    public double ExternalWallUValue { get; init; }
    public double WindowAreaM2Minimum { get; init; }
    public double WindowAreaFactor { get; init; }
    public double WindowUValue { get; init; }
    public double WindowShgc { get; init; }
    public CardinalDirection OddRoomOrientation { get; init; } = CardinalDirection.South;
    public CardinalDirection EvenRoomOrientation { get; init; } = CardinalDirection.East;
}
