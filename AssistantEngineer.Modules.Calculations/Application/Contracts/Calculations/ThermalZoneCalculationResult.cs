namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class ThermalZoneCalculationResult
{
    public int? ThermalZoneId { get; set; }
    public string ThermalZoneName { get; set; } = string.Empty;
    public bool IsUnassignedRoomsZone { get; set; }
    public int RoomsCount { get; set; }
    public int? PeakHour { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public List<int> RoomIds { get; set; } = new();
    public List<double> HourlyHeatLoadW { get; set; } = new();
}
