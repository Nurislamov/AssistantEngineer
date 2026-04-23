namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed class NaturalVentilationPreviewResponse
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;

    public double IndoorTemperatureC { get; set; }
    public double OutdoorTemperatureC { get; set; }
    public double WindSpeedMPerS { get; set; }
    public double DemandFactor { get; set; }
    public int HourOfDay { get; set; }

    public bool IsOpen { get; set; }
    public double OpeningFactor { get; set; }
    public double EffectiveOpeningAreaM2 { get; set; }
    public string Reason { get; set; } = string.Empty;

    public double HeatTransferCoefficientWPerK { get; set; }
    public double EquivalentAirChangesPerHour { get; set; }
}