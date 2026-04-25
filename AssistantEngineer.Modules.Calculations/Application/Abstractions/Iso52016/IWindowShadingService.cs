using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Models.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface IWindowShadingService
{
    double CalculateCombinedSolarReduction(
        CardinalDirection orientation,
        double latitudeDegrees,
        int dayOfYear,
        int hourOfDay,
        WindowShadingOptions options);
}
