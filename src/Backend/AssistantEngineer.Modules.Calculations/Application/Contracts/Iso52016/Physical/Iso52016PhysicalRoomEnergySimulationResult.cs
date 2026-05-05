using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Result for the ISO 52016-inspired physical room simulation service adapter.
/// It intentionally exposes the physical request, generated Matrix request and Matrix profile for traceable engineering review.
/// </summary>
public sealed record Iso52016PhysicalRoomEnergySimulationResult(
    string RoomCode,
    Iso52016PhysicalRoomModelRequest PhysicalModelRequest,
    Iso52016MatrixHourlySolverRequest MatrixSolverRequest,
    Iso52016MatrixHourlySolverProfile MatrixSolverProfile)
{
    public int HourCount => MatrixSolverProfile.HourCount;

    public double AnnualHeatingEnergyKWh => MatrixSolverProfile.AnnualHeatingEnergyKWh;

    public double AnnualCoolingEnergyKWh => MatrixSolverProfile.AnnualCoolingEnergyKWh;

    public double PeakHeatingLoadW => MatrixSolverProfile.PeakHeatingLoadW;

    public double PeakCoolingLoadW => MatrixSolverProfile.PeakCoolingLoadW;
}