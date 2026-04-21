namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class BuildingHeatingLoadResult
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int RoomsCount { get; set; }

    public double TransmissionHeatLossW { get; set; }
    public double VentilationHeatLossW { get; set; }
    public double TotalDesignHeatingLoadW { get; set; }
    public double TotalDesignHeatingLoadKw { get; set; }

    public List<RoomHeatingLoadResult> Rooms { get; set; } = new();
}
