namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed class NaturalVentilationPreviewRequest
{
    public double IndoorTemperatureC { get; set; }
    public double OutdoorTemperatureC { get; set; }
    public double WindSpeedMPerS { get; set; }
    public double DemandFactor { get; set; }
    public int HourOfDay { get; set; }
}