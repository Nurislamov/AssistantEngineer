namespace AssistantEngineer.Domain.Models;

public class Wall
{
    public int Id { get; set; }
    public int RoomId { get; set; }

    public double AreaM2 { get; set; }
    public bool IsExternal { get; set; }

    public Room Room { get; set; } = null!;
}