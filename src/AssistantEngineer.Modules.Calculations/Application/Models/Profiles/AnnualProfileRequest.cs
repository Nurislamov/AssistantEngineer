namespace AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

public sealed record AnnualProfileRequest(
    string Name,
    int Year,
    IReadOnlyList<double> WeekdayProfile,
    IReadOnlyList<double> WeekendProfile,
    IReadOnlySet<DateOnly> HolidayDates);