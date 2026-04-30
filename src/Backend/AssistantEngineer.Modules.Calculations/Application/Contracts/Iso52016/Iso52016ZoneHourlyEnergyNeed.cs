namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016ZoneHourlyEnergyNeed(
    string ZoneName,
    int HourOfYear,
    int Month,
    double HeatingLoadW,
    double CoolingLoadW,
    double OperativeTemperatureC,
    double OutdoorTemperatureC,
    double InternalGainsW,
    double SolarGainsW,
    double TransmissionW = 0,
    double VentilationW = 0,
    double InfiltrationW = 0,
    double GroundW = 0,
    double TransmissionBalanceW = 0,
    double VentilationBalanceW = 0,
    double InfiltrationBalanceW = 0,
    double GroundBalanceW = 0);