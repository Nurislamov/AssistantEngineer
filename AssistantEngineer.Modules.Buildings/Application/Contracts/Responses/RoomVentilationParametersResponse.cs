namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class RoomVentilationParametersResponse
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;

    public double AirChangesPerHour { get; set; }
    public double HeatRecoveryEfficiency { get; set; }
    public double InfiltrationAirChangesPerHour { get; set; }
    public double WindExposureFactor { get; set; }
    public double StackCoefficient { get; set; }
    public double WindCoefficient { get; set; }
}