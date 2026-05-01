using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundTemperatureService
{
    double[] BuildHourlyProfile(IReadOnlyList<AnnualHourlyData> hourlyClimateData);
    double GetMonthlyAverageTemperature(IReadOnlyList<AnnualHourlyData> hourlyClimateData, int month);
}
