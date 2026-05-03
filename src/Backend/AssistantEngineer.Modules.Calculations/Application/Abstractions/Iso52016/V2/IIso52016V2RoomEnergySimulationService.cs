using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016V2RoomEnergySimulationService
{
    Result<Iso52016V2RoomEnergySimulationResult> Simulate(
        Iso52016RoomEnergySimulationRequest request);
}