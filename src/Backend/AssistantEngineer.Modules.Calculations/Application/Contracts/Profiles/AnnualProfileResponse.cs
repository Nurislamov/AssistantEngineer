using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;

public sealed class AnnualProfileResponse
{
    public int Year { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public RoomTypeDto RoomType { get; set; }
    public AnnualProfileKindDto ProfileKind { get; set; }
    public int TotalHours { get; set; }
    public int HolidayDaysCount { get; set; }
    public int WeekendDaysCount { get; set; }
    public int WorkingDaysCount { get; set; }
    public double AnnualAverageValue { get; set; }
    public List<double> HourlyValues { get; set; } = new();
}