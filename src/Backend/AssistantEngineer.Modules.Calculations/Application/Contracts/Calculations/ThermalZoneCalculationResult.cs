namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class ThermalZoneCalculationResult
{
    public int? ThermalZoneId { get; set; }
    public string ThermalZoneName { get; set; } = string.Empty;
    public bool IsUnassignedRoomsZone { get; set; }
    public int RoomsCount { get; set; }
    public int? PeakHourOfYear { get; set; }

    [Obsolete("Use PeakHourOfYear.")]
    public int? PeakHour
    {
        get => PeakHourOfYear;
        set => PeakHourOfYear = value;
    }

    public double CoolingLoadW { get; set; }
    public double CoolingLoadKw { get; set; }

    [Obsolete("Use CoolingLoadW.")]
    public double TotalHeatLoadW
    {
        get => CoolingLoadW;
        set => CoolingLoadW = value;
    }

    [Obsolete("Use CoolingLoadKw.")]
    public double TotalHeatLoadKw
    {
        get => CoolingLoadKw;
        set => CoolingLoadKw = value;
    }

    public List<int> RoomIds { get; set; } = new();
    public List<double> HourlyHeatLoadW { get; set; } = new();
}
