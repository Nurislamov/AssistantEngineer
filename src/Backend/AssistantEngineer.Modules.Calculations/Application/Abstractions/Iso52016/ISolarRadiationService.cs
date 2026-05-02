using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISolarRadiationService
{
    double CalculateVerticalSurfaceRadiation(
        AnnualHourlyData hourlyData,
        CardinalDirection orientation,
        double latitude,
        int dayOfYear,
        int hour);
}
