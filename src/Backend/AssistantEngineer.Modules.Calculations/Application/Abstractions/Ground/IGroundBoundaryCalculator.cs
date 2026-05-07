using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundBoundaryCalculator
{
    GroundBoundaryCalculationResult Calculate(GroundBoundaryCalculationInput input);
}
