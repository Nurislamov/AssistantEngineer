using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

public sealed class AnnualScheduleGenerationService : IAnnualScheduleGenerator
{
    private readonly IHolidayCalendarProvider _holidays;
    private readonly IAnnualProfileTemplateProvider _templates;

    public AnnualScheduleGenerationService(
        IHolidayCalendarProvider holidays,
        IAnnualProfileTemplateProvider templates)
    {
        _holidays = holidays;
        _templates = templates;
    }

    public double[] Generate(
        int year,
        string countryCode,
        RoomType roomType,
        AnnualProfileKind profileKind)
    {
        var template = _templates.GetTemplate(roomType, profileKind);
        var holidayDates = _holidays.GetHolidays(countryCode, year)
            .Select(h => h.Date)
            .ToHashSet();

        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hours = DateTime.IsLeapYear(year) ? 8784 : 8760;
        var result = new double[hours];

        for (var i = 0; i < hours; i++)
        {
            var current = start.AddHours(i);
            var date = DateOnly.FromDateTime(current);
            var hour = current.Hour;

            var source = SelectDayProfile(date, template, holidayDates);
            result[i] = source[hour];
        }

        return result;
    }
    private static double[] SelectDayProfile(
        DateOnly date,
        Models.Profiles.DailyProfileTemplate template,
        HashSet<DateOnly> holidays)
    {
        if (holidays.Contains(date))
            return template.Holiday;

        return date.DayOfWeek switch
        {
            DayOfWeek.Saturday or DayOfWeek.Sunday => template.Weekend,
            _ => template.WorkingDay
        };
    }
}