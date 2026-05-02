using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyRoomWindowSolarGainRecord(
    string WindowCode,
    CardinalDirection Orientation,
    string SurfaceCode,
    double WindowAreaM2,
    double EffectiveGlazingAreaM2,
    double BeamSolarGainW,
    double DiffuseSkySolarGainW,
    double GroundReflectedSolarGainW,
    double TotalSolarGainW);