using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

internal static class BuildingCalculationDataValidator
{
    public static Result ValidateHeatingLoadData(Building building)
    {
        if (building.ClimateZone is null)
            return Result.Validation("Building climate zone is required for EN 12831 heating load calculation.");

        if (!building.Floors.SelectMany(floor => floor.Rooms).Any())
            return Result.Validation("At least one room is required for EN 12831 heating load calculation.");

        return Result.Success();
    }
}
