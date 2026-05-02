namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;

public sealed class BuildingComfortMetricsResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public double OverheatingThresholdC { get; set; }
    public double SevereOverheatingThresholdC { get; set; }
    public double UnderheatingThresholdC { get; set; }

    public bool OccupiedHoursOnly { get; set; }
    public double OccupancyThreshold { get; set; }

    public int CoolingSeasonStartMonth { get; set; }
    public int CoolingSeasonEndMonth { get; set; }

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
    public int? PeakHourOfYear { get; set; }

    public List<MonthlyComfortMetricsResponse> Monthly { get; set; } = new();
}
