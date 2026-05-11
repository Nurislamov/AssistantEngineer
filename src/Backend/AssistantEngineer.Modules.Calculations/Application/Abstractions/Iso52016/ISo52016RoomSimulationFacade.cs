using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016RoomSimulationFacade
{
    Result<Iso52016RoomSimulationFacadeResult> Simulate(
        Iso52016RoomSimulationFacadeRequest request);
}