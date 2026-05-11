using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016RoomInternalGainProfileBuilder
{
    Result<Iso52016RoomInternalGainProfile> Build(
        Iso52016RoomInternalGainProfileRequest request);
}