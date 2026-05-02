using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface IIso52016RoomHourlyInputProfileBuilder
{
    Result<Iso52016RoomHourlyInputProfile> Build(
        Iso52016RoomHourlyInputProfileRequest request);
}