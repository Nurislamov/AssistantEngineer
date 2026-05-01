namespace AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;

public sealed record AnnualEnergyBalanceHourInput(
    int HourIndex,
    int Month,
    double HeatingLoadW,
    double CoolingLoadW,
    double TransmissionW = 0,
    double VentilationW = 0,
    double InfiltrationW = 0,
    double SolarGainsW = 0,
    double InternalGainsW = 0,
    double GroundW = 0,
    double HourDurationH = 1.0,
    double TransmissionBalanceW = 0,
    double VentilationBalanceW = 0,
    double InfiltrationBalanceW = 0,
    double GroundBalanceW = 0,
    double MechanicalVentilationW = 0,
    double NaturalVentilationW = 0,
    double MechanicalVentilationBalanceW = 0,
    double NaturalVentilationBalanceW = 0);
