namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class FloorCalculationResult
{
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int? PeakHour { get; set; }
    public int RoomsCount { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public List<double> HourlyHeatLoadW { get; set; } = new();
}
