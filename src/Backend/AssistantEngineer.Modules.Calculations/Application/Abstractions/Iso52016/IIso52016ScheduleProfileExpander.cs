using AssistantEngineer.Modules.Buildings.Domain.Schedules;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface IIso52016ScheduleProfileExpander
{
    IReadOnlyList<double> ExpandToAnnualProfile(
        HourlySchedule? schedule,
        int hourCount,
        double defaultFactor);
}