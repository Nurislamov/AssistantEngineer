namespace AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

public sealed record RoomLoadFixedComponentInput(
    double HeatingTransmissionW = 0,
    double HeatingWindowTransmissionW = 0,
    double HeatingGroundW = 0,
    double HeatingVentilationW = 0,
    double HeatingInfiltrationW = 0,
    double CoolingTransmissionW = 0,
    double CoolingWindowTransmissionW = 0,
    double CoolingGroundW = 0,
    double CoolingVentilationW = 0,
    double CoolingInfiltrationW = 0,
    double CoolingSolarW = 0,
    double CoolingInternalGainsW = 0,
    double HeatingUsefulSolarGainOffsetW = 0,
    double HeatingUsefulInternalGainOffsetW = 0);

