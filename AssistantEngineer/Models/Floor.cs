namespace AssistantEngineer.Models;

public class Floor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    public List<Room> Rooms { get; set; } = [];
}