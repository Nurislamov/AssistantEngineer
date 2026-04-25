namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed class DomesticHotWaterDemandRequest
{
    public int PeopleCount { get; set; }
    public double LitersPerPersonDay { get; set; } = 40;
    public double ColdWaterTemperatureC { get; set; } = 10;
    public double HotWaterTemperatureC { get; set; } = 60;
    public int Year { get; set; } = 2020;
    public double DistributionLossFactor { get; set; } = 0.1;
    public double StorageLossKWhPerDay { get; set; }
    public double CirculationLossKWhPerDay { get; set; }
    public bool IncludeHourlyProfile { get; set; }
    public IReadOnlyList<double>? WeekdayDrawProfile { get; set; }
    public IReadOnlyList<double>? WeekendDrawProfile { get; set; }
    public HashSet<DateOnly> HolidayDates { get; set; } = new();
}