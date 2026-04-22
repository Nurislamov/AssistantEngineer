using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Domain.ThermalZones;

public class ThermalZoneRoom
{
    public int ThermalZoneId { get; private set; }
    public ThermalZone ThermalZone { get; private set; } = null!;
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    private ThermalZoneRoom() { }

    private ThermalZoneRoom(Room room)
    {
        Room = room;
        RoomId = room.Id;
    }

    internal static ThermalZoneRoom Create(Room room) => new(room);
}
