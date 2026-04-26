using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analysis;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

public sealed class HourlyRoomProfileAccessor : IHourlyRoomProfileAccessor
{
    private readonly IRoomAnnualProfileSetProvider _annualProfiles;

    public HourlyRoomProfileAccessor(IRoomAnnualProfileSetProvider annualProfiles)
    {
        _annualProfiles = annualProfiles;
    }

    public RoomHourlyProfileSnapshot GetSnapshot(
        Room room,
        int hourOfYear,
        AnnualProfileOptionsDto? annualProfileOptions)
    {
        if (annualProfileOptions is null || !annualProfileOptions.UseAnnualProfiles)
            return BuildNeutralFallback();

        var set = _annualProfiles.GetProfiles(
            room,
            annualProfileOptions.Year,
            annualProfileOptions.CountryCode);

        if (set.TotalHours == 0)
            return BuildNeutralFallback();

        var safeHour = Math.Clamp(hourOfYear, 0, set.TotalHours - 1);

        return new RoomHourlyProfileSnapshot
        {
            Occupancy = set.Occupancy[safeHour],
            Equipment = set.Equipment[safeHour],
            Lighting = set.Lighting[safeHour],
            Dhw = set.Dhw[safeHour]
        };
    }

    private static RoomHourlyProfileSnapshot BuildNeutralFallback()
    {
        return new RoomHourlyProfileSnapshot
        {
            Occupancy = 1.0,
            Equipment = 1.0,
            Lighting = 1.0,
            Dhw = 1.0
        };
    }
}