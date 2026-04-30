namespace AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

public sealed record RoomHeatingLoadBreakdown(
    double TransmissionW,
    double WindowTransmissionW,
    double GroundW,
    double VentilationW,
    double InfiltrationW,
    double UsefulSolarGainOffsetW,
    double UsefulInternalGainOffsetW)
{
    public double TotalW =>
        TransmissionW +
        WindowTransmissionW +
        GroundW +
        VentilationW +
        InfiltrationW -
        UsefulSolarGainOffsetW -
        UsefulInternalGainOffsetW;
}
