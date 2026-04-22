using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IFloorsFacade
{
    Task<Result<FloorResponse>> CreateAsync(
        int buildingId,
        CreateFloorRequest request,
        CancellationToken cancellationToken);

    Task<Result<List<FloorResponse>>> GetByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken);

    Task<Result<FloorResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<FloorCalculationResult>> CalculateCoolingLoadAsync(
        int id,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);
}
