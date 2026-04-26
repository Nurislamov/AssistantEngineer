using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IRoomAnnualProfileSetProvider
{
    RoomAnnualProfileSet GetProfiles(Room room, int year, string countryCode);
}