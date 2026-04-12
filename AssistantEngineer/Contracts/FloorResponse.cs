namespace AssistantEngineer.Contracts;

public class FloorResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BuildingId { get; set; }
    public double ReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }
}
