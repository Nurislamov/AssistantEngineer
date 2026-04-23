namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;

public sealed class ZoneComfortMetricsResponse
{
    public string ZoneName { get; set; } = string.Empty;

    public int HoursEvaluated { get; set; }
    public int OccupiedHoursDetected { get; set; }

    public int HoursAboveOverheatingThreshold { get; set; }
    public double DegreeHoursAboveOverheatingThreshold { get; set; }

    public int HoursAboveSevereOverheatingThreshold { get; set; }
    public double DegreeHoursAboveSevereOverheatingThreshold { get; set; }

    public int HoursBelowUnderheatingThreshold { get; set; }
    public double DegreeHoursBelowUnderheatingThreshold { get; set; }

    public int CoolingSeasonHoursAboveOverheatingThreshold { get; set; }
    public double CoolingSeasonDegreeHoursAboveOverheatingThreshold { get; set; }

    public double PeakOperativeTemperatureC { get; set; }
    public int PeakHourOfYear { get; set; }

    public List<MonthlyComfortMetricsResponse> Monthly { get; set; } = new();
}