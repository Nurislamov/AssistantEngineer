using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

public sealed class RoomAnnualProfileSetProvider : IRoomAnnualProfileSetProvider
{
    private readonly IAnnualScheduleGenerator _annualScheduleGenerator;

    public RoomAnnualProfileSetProvider(IAnnualScheduleGenerator annualScheduleGenerator)
    {
        _annualScheduleGenerator = annualScheduleGenerator;
    }

    public RoomAnnualProfileSet GetProfiles(Room room, int year, string countryCode)
    {
        return new RoomAnnualProfileSet
        {
            Occupancy = _annualScheduleGenerator.Generate(year, countryCode, room.Type, AnnualProfileKind.Occupancy),
            Equipment = _annualScheduleGenerator.Generate(year, countryCode, room.Type, AnnualProfileKind.Equipment),
            Lighting = _annualScheduleGenerator.Generate(year, countryCode, room.Type, AnnualProfileKind.Lighting),
            Dhw = _annualScheduleGenerator.Generate(year, countryCode, room.Type, AnnualProfileKind.Dhw)
        };
    }
}