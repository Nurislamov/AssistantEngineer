using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analysis;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IHourlyRoomProfileAccessor
{
    RoomHourlyProfileSnapshot GetSnapshot(
        Room room,
        int hourOfYear,
        AnnualProfileOptionsDto? annualProfileOptions);
}