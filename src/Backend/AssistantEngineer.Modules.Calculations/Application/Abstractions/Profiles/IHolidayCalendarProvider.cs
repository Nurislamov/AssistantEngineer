using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IHolidayCalendarProvider
{
    IReadOnlyCollection<HolidayCalendarDay> GetHolidays(string countryCode, int year);
}