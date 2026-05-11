using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;

public interface ISo52016MatrixRoomEnergySimulationResultMapper
{
    Result<Iso52016RoomEnergySimulationResult> Map(
        Iso52016MatrixRoomEnergySimulationResult source);
}