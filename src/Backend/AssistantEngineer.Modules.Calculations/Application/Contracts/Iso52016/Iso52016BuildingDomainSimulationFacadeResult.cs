using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016BuildingDomainSimulationFacadeResult(
    int BuildingId,
    string BuildingName,
    IReadOnlyList<Room> Rooms,
    Iso52016BuildingSimulationFacadeResult SimulationResult)
{
    public int RoomCount => Rooms.Count;

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