using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Domain.Models.ThermalZones;

public class ThermalZoneRoom
{
    public int ThermalZoneId { get; private set; }
    public ThermalZone ThermalZone { get; private set; } = null!;
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    private ThermalZoneRoom() { }

    private ThermalZoneRoom(int roomId)
    {
        RoomId = roomId;
    }

    internal static ThermalZoneRoom Create(int roomId) => new(roomId);
}
