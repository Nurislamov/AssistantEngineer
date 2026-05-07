using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IBuildingGroundBoundaryCalculator
{
    BuildingGroundBoundaryCalculationResult Calculate(BuildingGroundBoundaryCalculationInput input);
}
