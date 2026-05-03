using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016V2RoomEnergySimulationResultMapper
{
    Result<Iso52016RoomEnergySimulationResult> Map(
        Iso52016V2RoomEnergySimulationResult source);
}