namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomSimulationFacadeResult(
    string RoomCode,
    Iso52016WeatherSolarContext WeatherSolarContext,
    Iso52016RoomEnergySimulationRequest SimulationRequest,
    Iso52016RoomEnergySimulationResult SimulationResult)
{
    public int HourCount => SimulationResult.HourCount;

    public double AnnualHeatingEnergyKWh =>
        SimulationResult.AnnualHeatingEnergyKWh;

    public double AnnualCoolingEnergyKWh =>
        SimulationResult.AnnualCoolingEnergyKWh;

    public double AnnualSolarGainsKWh =>
        SimulationResult.AnnualSolarGainsKWh;

    public double AnnualInternalGainsKWh =>
        SimulationResult.AnnualInternalGainsKWh;

    public double AnnualTotalGainsKWh =>
        SimulationResult.AnnualTotalGainsKWh;

    public double PeakHeatingLoadW =>
        SimulationResult.PeakHeatingLoadW;

    public double PeakCoolingLoadW =>
        SimulationResult.PeakCoolingLoadW;
}