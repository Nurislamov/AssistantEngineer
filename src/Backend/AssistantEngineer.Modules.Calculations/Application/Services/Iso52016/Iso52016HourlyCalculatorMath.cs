using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal static class Iso52016HourlyCalculatorMath
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public static bool HasCompleteAnnualWeatherData(AnnualClimateData? annualData) =>
        annualData is not null &&
        annualData.HourlyData
            .Select(hour => hour.HourOfYear)
            .Distinct()
            .OrderBy(hour => hour)
            .SequenceEqual(Enumerable.Range(0, 8760));

    public static double WeightedAverage(IReadOnlyCollection<Room> rooms, Func<Room, double> valueSelector)
    {
        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        if (totalArea <= 0)
            return rooms.Count == 0 ? 0 : rooms.Average(valueSelector);

        return rooms.Sum(room => valueSelector(room) * room.Area.SquareMeters) / totalArea;
    }

    public static double GetWallUValue(Wall wall) =>
        wall.ConstructionAssembly is { UValueWPerM2K: > 0 } assembly
            ? assembly.UValueWPerM2K
            : wall.UValue.Value;

    public static double GetPeopleHeatGain(RoomType type) => type switch
    {
        RoomType.Office => 125,
        RoomType.MeetingRoom => 125,
        RoomType.Corridor => 80,
        RoomType.ServerRoom => 125,
        RoomType.Retail => 170,
        RoomType.Residential => 80,
        _ => 125
    };

    public static int GetMonth(int hourOfYear)
    {
        var dayOfYear = hourOfYear / 24;
        var accumulatedDays = 0;

        for (var month = 1; month <= DaysPerMonth.Length; month++)
        {
            accumulatedDays += DaysPerMonth[month - 1];
            if (dayOfYear < accumulatedDays)
                return month;
        }

        return 12;
    }

    public static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
