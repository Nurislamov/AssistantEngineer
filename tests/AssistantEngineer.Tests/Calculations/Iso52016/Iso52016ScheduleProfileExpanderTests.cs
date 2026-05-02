using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016ScheduleProfileExpanderTests
{
    private readonly Iso52016ScheduleProfileExpander _expander = new();

    [Fact]
    public void ExpandToAnnualProfile_WhenScheduleIsNull_ReturnsDefaultFactor()
    {
        var result = _expander.ExpandToAnnualProfile(
            schedule: null,
            hourCount: 48,
            defaultFactor: 0.75);

        Assert.Equal(48, result.Count);
        Assert.All(result, factor => Assert.Equal(0.75, factor));
    }

    [Fact]
    public void ExpandToAnnualProfile_RepeatsDailySchedule()
    {
        var scheduleResult = HourlySchedule.Create(
            "Office",
            Enumerable.Range(0, 24)
                .Select(hour => hour / 23.0)
                .ToArray());

        Assert.True(scheduleResult.IsSuccess);

        var result = _expander.ExpandToAnnualProfile(
            scheduleResult.Value,
            hourCount: 48,
            defaultFactor: 1.0);

        Assert.Equal(48, result.Count);
        Assert.Equal(0.0, result[0]);
        Assert.Equal(1.0, result[23]);
        Assert.Equal(0.0, result[24]);
        Assert.Equal(1.0, result[47]);
    }
}