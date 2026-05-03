using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;

public interface IIso52016MatrixRoomEnergySimulationService
{
    Result<Iso52016MatrixRoomEnergySimulationResult> Simulate(
        Iso52016RoomEnergySimulationRequest request);
}