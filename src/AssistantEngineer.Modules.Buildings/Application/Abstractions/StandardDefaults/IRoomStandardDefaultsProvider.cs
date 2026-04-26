using AssistantEngineer.Modules.Buildings.Application.Models.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;

public interface IRoomStandardDefaultsProvider
{
    RoomStandardDefaults GetDefaults(Room room);
}