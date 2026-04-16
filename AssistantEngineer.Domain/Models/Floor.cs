namespace AssistantEngineer.Domain.Models;

public class Floor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public List<Room> Rooms { get; set; } = [];
}
