using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016BuildingRoomCollector
{
    Result<IReadOnlyList<Room>> CollectRooms(
        Building building);
}