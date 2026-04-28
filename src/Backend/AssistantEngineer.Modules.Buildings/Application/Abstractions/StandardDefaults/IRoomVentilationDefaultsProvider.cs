using AssistantEngineer.Modules.Buildings.Application.Models.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;

public interface IRoomVentilationDefaultsProvider
{
    RoomVentilationDefaults GetDefaults(Room room);
}