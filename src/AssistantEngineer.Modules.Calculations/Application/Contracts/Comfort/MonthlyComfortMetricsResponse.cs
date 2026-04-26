namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;

public sealed class MonthlyComfortMetricsResponse
{
    public int Month { get; set; }
    public int HoursEvaluated { get; set; }
    public int OccupiedHoursDetected { get; set; }

    public int HoursAboveOverheatingThreshold { get; set; }
    public double DegreeHoursAboveOverheatingThreshold { get; set; }

    public int HoursAboveSevereOverheatingThreshold { get; set; }
    public double DegreeHoursAboveSevereOverheatingThreshold { get; set; }

    public int HoursBelowUnderheatingThreshold { get; set; }
    public double DegreeHoursBelowUnderheatingThreshold { get; set; }

    public double PeakOperativeTemperatureC { get; set; }
}