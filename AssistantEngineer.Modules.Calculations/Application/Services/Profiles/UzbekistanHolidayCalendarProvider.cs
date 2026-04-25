using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

public sealed class UzbekistanHolidayCalendarProvider : IHolidayCalendarProvider
{
    public IReadOnlyCollection<HolidayCalendarDay> GetHolidays(string countryCode, int year)
    {
        if (!string.Equals(countryCode, "UZ", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<HolidayCalendarDay>();

        // Seed/public-holiday baseline. Can be replaced later by JSON/API.
        return new List<HolidayCalendarDay>
        {
            new(new DateOnly(year, 1, 1), "New Year"),
            new(new DateOnly(year, 1, 14), "Defenders of the Motherland Day"),
            new(new DateOnly(year, 3, 8), "International Women's Day"),
            new(new DateOnly(year, 3, 21), "Navruz"),
            new(new DateOnly(year, 5, 9), "Day of Memory and Honor"),
            new(new DateOnly(year, 9, 1), "Independence Day"),
            new(new DateOnly(year, 10, 1), "Teachers and Mentors Day"),
            new(new DateOnly(year, 12, 8), "Constitution Day")
        };
    }
}