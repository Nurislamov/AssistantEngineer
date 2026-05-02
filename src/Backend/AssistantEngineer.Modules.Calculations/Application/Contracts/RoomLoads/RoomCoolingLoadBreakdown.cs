namespace AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

public sealed record RoomCoolingLoadBreakdown(
    double TransmissionW,
    double WindowTransmissionW,
    double SolarW,
    double VentilationW,
    double InfiltrationW,
    double InternalGainsW,
    double GroundW)
{
    public double TotalW =>
        TransmissionW +
        WindowTransmissionW +
        SolarW +
        VentilationW +
        InfiltrationW +
        InternalGainsW +
        GroundW;
}
