using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016RoomEnergySimulationRequestBuilder
{
    Result<Iso52016RoomEnergySimulationRequest> Build(
        Iso52016RoomEnergySimulationBuildRequest request);
}