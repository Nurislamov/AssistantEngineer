using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

public sealed class AnnualProfileGenerationService
{
    private readonly IAnnualScheduleGenerator _generator;
    private readonly IHolidayCalendarProvider _holidays;

    public AnnualProfileGenerationService(
        IAnnualScheduleGenerator generator,
        IHolidayCalendarProvider holidays)
    {
        _generator = generator;
        _holidays = holidays;
    }

    public AnnualProfileResponse Generate(AnnualProfileGenerationRequest request)
    {
        var roomType = request.RoomType.ToDomain();
        var profileKind = request.ProfileKind.ToDomain();
        var hourly = _generator.Generate(request.Year, request.CountryCode, roomType, profileKind);
        var holidays = _holidays.GetHolidays(request.CountryCode, request.Year)
            .Select(h => h.Date)
            .ToHashSet();

        var workingDays = 0;
        var weekendDays = 0;
        var holidayDays = 0;

        var start = new DateOnly(request.Year, 1, 1);
        var totalDays = DateTime.IsLeapYear(request.Year) ? 366 : 365;
        for (var i = 0; i < totalDays; i++)
        {
            var day = start.AddDays(i);
            if (holidays.Contains(day))
            {
                holidayDays++;
                continue;
            }

            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                weekendDays++;
            else
                workingDays++;
        }

        return new AnnualProfileResponse
        {
            Year = request.Year,
            CountryCode = request.CountryCode,
            RoomType = roomType.ToContract(),
            ProfileKind = request.ProfileKind,
            TotalHours = hourly.Length,
            HolidayDaysCount = holidayDays,
            WeekendDaysCount = weekendDays,
            WorkingDaysCount = workingDays,
            AnnualAverageValue = hourly.Length == 0 ? 0 : Math.Round(hourly.Average(), 6),
            HourlyValues = hourly.ToList()
        };
    }
}

internal static class AnnualProfileKindMapper
{
    public static AnnualProfileKind ToDomain(this AnnualProfileKindDto dto) => dto switch
    {
        AnnualProfileKindDto.Occupancy => AnnualProfileKind.Occupancy,
        AnnualProfileKindDto.Equipment => AnnualProfileKind.Equipment,
        AnnualProfileKindDto.Lighting => AnnualProfileKind.Lighting,
        AnnualProfileKindDto.Dhw => AnnualProfileKind.Dhw,
        _ => AnnualProfileKind.Occupancy
    };
}