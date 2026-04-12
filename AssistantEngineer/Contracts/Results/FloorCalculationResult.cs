namespace AssistantEngineer.Contracts.Results;

public class FloorCalculationResult
{
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public int RoomsCount { get; set; }
    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public double ReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }
}
