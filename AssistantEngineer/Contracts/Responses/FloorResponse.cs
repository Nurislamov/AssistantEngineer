namespace AssistantEngineer.Contracts.Responses;

public class FloorResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BuildingId { get; set; }
    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }
}
