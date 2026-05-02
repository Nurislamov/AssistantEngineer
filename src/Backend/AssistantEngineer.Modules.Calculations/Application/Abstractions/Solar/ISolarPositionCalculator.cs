using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;

public interface ISolarPositionCalculator
{
    SolarPositionResult Calculate(
        SolarPositionRequest request);
}