namespace AssistantEngineer.Contracts.Results;

public class BuildingCalculationResult
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int FloorsCount { get; set; }
    public int RoomsCount { get; set; }
    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }
}