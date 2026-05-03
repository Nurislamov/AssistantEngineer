using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;

public interface ISolarTimeCalculator
{
    SolarTimeResult Calculate(
        DateTimeOffset timestamp,
        SolarLocation location);
}
