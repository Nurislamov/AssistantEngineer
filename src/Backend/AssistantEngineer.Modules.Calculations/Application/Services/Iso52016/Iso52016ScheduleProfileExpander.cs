using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016ScheduleProfileExpander : ISo52016ScheduleProfileExpander
{
    public IReadOnlyList<double> ExpandToAnnualProfile(
        HourlySchedule? schedule,
        int hourCount,
        double defaultFactor)
    {
        if (hourCount <= 0)
            return Array.Empty<double>();

        var dailyFactors = schedule?.Factors;

        if (dailyFactors is null || dailyFactors.Count == 0)
        {
            return Enumerable
                .Repeat(defaultFactor, hourCount)
                .ToArray();
        }

        if (dailyFactors.Count != 24)
        {
            throw new InvalidOperationException(
                "Hourly schedule must contain exactly 24 factors.");
        }

        var result = new double[hourCount];

        for (var hour = 0; hour < hourCount; hour++)
        {
            result[hour] = dailyFactors[hour % 24];
        }

        return result;
    }
}