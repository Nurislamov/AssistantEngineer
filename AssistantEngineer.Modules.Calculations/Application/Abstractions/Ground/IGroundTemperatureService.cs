using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundTemperatureService
{
    double[] BuildHourlyProfile(IReadOnlyList<HourlyClimateData> hourlyClimateData);
    double GetMonthlyAverageTemperature(IReadOnlyList<HourlyClimateData> hourlyClimateData, int month);
}