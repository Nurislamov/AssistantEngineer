using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016MatrixRoomEnergySimulationResult(
    string RoomCode,
    Iso52016RoomSolarGainProfile SolarGainProfile,
    Iso52016RoomInternalGainProfile InternalGainProfile,
    Iso52016RoomHourlyInputProfile HourlyInputProfile,
    Iso52016MatrixHourlySolverRequest MatrixSolverRequest,
    Iso52016MatrixHourlySolverProfile MatrixSolverProfile)
{
    public int HourCount => MatrixSolverProfile.HourCount;

    public double AnnualHeatingEnergyKWh => MatrixSolverProfile.AnnualHeatingEnergyKWh;

    public double AnnualCoolingEnergyKWh => MatrixSolverProfile.AnnualCoolingEnergyKWh;

    public double AnnualSolarGainsKWh => SolarGainProfile.AnnualSolarGainsKWh;

    public double AnnualInternalGainsKWh => InternalGainProfile.AnnualInternalGainsKWh;

    public double AnnualTotalGainsKWh => HourlyInputProfile.AnnualTotalGainsKWh;

    public double PeakHeatingLoadW => MatrixSolverProfile.PeakHeatingLoadW;

    public double PeakCoolingLoadW => MatrixSolverProfile.PeakCoolingLoadW;
}