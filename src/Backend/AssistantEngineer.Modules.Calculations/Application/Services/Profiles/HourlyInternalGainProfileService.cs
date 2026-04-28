using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analysis;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

public sealed class HourlyInternalGainProfileService
{
    private readonly IHourlyRoomProfileAccessor _profiles;

    public HourlyInternalGainProfileService(IHourlyRoomProfileAccessor profiles)
    {
        _profiles = profiles;
    }

    public RoomHourlyProfileSnapshot GetRoomHourlyMultipliers(
        Room room,
        int hourOfYear,
        AnnualProfileOptionsDto? annualProfileOptions)
    {
        return _profiles.GetSnapshot(room, hourOfYear, annualProfileOptions);
    }
}