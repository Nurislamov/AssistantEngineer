using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface IIso52016RoomSolarGainProfileBuilder
{
    Result<Iso52016RoomSolarGainProfile> Build(
        Iso52016RoomSolarGainProfileRequest request);
}