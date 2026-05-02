namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyRoomSolarGainRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double BeamSolarGainW,
    double DiffuseSkySolarGainW,
    double GroundReflectedSolarGainW,
    double TotalSolarGainW,
    IReadOnlyList<Iso52016HourlyRoomWindowSolarGainRecord> WindowGains);