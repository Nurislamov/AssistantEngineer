namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class SyntheticDesignDayHourResponse
{
    public int HourOfDay { get; set; }
    public int DayOfYear { get; set; }
    public int Month { get; set; }

    public double OutdoorTemperatureC { get; set; }
    public double SolarHorizontalIrradianceWPerM2 { get; set; }

    public double LoadW { get; set; }
    public double LoadKw { get; set; }

    public double OperativeTemperatureC { get; set; }
    public double InternalGainsW { get; set; }
    public double SolarGainsW { get; set; }
}