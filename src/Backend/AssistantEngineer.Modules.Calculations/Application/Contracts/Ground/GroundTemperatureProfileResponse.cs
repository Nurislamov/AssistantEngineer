namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed class GroundTemperatureProfileResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public int TotalHours { get; set; }

    public double AnnualAverageGroundTemperatureC { get; set; }
    public double MinimumGroundTemperatureC { get; set; }
    public int MinimumHourOfYear { get; set; }
    public double MaximumGroundTemperatureC { get; set; }
    public int MaximumHourOfYear { get; set; }

    public List<double> HourlyValues { get; set; } = new();
    public List<GroundTemperatureMonthlyPoint> MonthlyAverages { get; set; } = new();
}

public sealed class GroundTemperatureMonthlyPoint
{
    public int Month { get; set; }
    public double AverageTemperatureC { get; set; }
}