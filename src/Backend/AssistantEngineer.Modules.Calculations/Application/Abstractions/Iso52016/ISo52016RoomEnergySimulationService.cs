using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016RoomEnergySimulationService
{
    Result<Iso52016RoomEnergySimulationResult> Simulate(
        Iso52016RoomEnergySimulationRequest request);
}