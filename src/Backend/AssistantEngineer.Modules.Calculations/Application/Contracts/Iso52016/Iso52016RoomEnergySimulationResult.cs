namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomEnergySimulationResult(
    string RoomCode,
    Iso52016RoomSolarGainProfile SolarGainProfile,
    Iso52016RoomInternalGainProfile InternalGainProfile,
    Iso52016RoomHourlyInputProfile HourlyInputProfile,
    Iso52016RoomHeatBalanceProfile HeatBalanceProfile)
{
    public int HourCount => HeatBalanceProfile.HourCount;

    public double AnnualHeatingEnergyKWh =>
        HeatBalanceProfile.AnnualHeatingEnergyKWh;

    public double AnnualCoolingEnergyKWh =>
        HeatBalanceProfile.AnnualCoolingEnergyKWh;

    public double AnnualSolarGainsKWh =>
        SolarGainProfile.AnnualSolarGainsKWh;

    public double AnnualInternalGainsKWh =>
        InternalGainProfile.AnnualInternalGainsKWh;

    public double AnnualTotalGainsKWh =>
        HeatBalanceProfile.AnnualTotalGainsKWh;

    public double PeakHeatingLoadW =>
        HeatBalanceProfile.PeakHeatingLoadW;

    public double PeakCoolingLoadW =>
        HeatBalanceProfile.PeakCoolingLoadW;
}