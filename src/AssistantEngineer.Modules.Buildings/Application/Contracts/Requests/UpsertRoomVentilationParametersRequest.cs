namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public sealed class UpsertRoomVentilationParametersRequest
{
    public double AirChangesPerHour { get; set; }
    public double HeatRecoveryEfficiency { get; set; }
    public double InfiltrationAirChangesPerHour { get; set; }
    public double WindExposureFactor { get; set; }
    public double StackCoefficient { get; set; }
    public double WindCoefficient { get; set; }
}