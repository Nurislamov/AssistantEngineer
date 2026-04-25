namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class BuildingCalculationResult
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int? PeakHour { get; set; }
    public int FloorsCount { get; set; }
    public int RoomsCount { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public List<double> HourlyHeatLoadW { get; set; } = new();
    public List<ThermalZoneCalculationResult> ThermalZones { get; set; } = new();
}
